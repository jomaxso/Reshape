<template>
    <div :class="['mode-panel', 'vacation-mode', { active: isActive }]">
        <div class="panel-header" @click="toggleMode">
            <div class="header-left">
                <span class="mode-icon">🏖️</span>
                <h3>Urlaubsmodus</h3>
            </div>
            <div class="header-right">
                <span v-if="stats.toRename > 0 && isActive" class="badge">{{ stats.toRename }}</span>
                <span class="toggle-icon">{{ isActive ? '▼' : '▶' }}</span>
            </div>
        </div>

        <Transition name="slide">
            <div v-if="isActive" class="panel-content">
                <!-- Configuration Section -->
                <div class="config-grid">
                    <div class="form-group">
                        <label for="startDate">Startdatum</label>
                        <input id="startDate" type="date" v-model="startDate" />
                        <span class="hint">Auto: Ältestes Datei-Datum</span>
                    </div>

                    <div class="form-group">
                        <label for="dayFolderPattern">Ordner-Muster</label>
                        <input id="dayFolderPattern" type="text" v-model="dayFolderPattern"
                            placeholder="Tag {day_number}" />
                    </div>

                    <div class="form-group full-width">
                        <label for="filePattern">Datei-Namens-Muster</label>
                        <input id="filePattern" type="text" v-model="localFilePattern"
                            placeholder="{year}-{month}-{day}_{counter:3}" />
                    </div>
                </div>

                <!-- Subfolder Toggle -->
                <div class="toggle-row">
                    <label class="toggle-switch">
                        <input type="checkbox" v-model="useSubfolders" @change="onSubfolderToggle" />
                        <span class="slider"></span>
                    </label>
                    <span class="toggle-label">Unterordner verwenden</span>
                </div>

                <Transition name="slide">
                    <div v-if="useSubfolders" class="form-group subfolder-input">
                        <label for="subfolderPattern">Unterordner-Muster</label>
                        <input id="subfolderPattern" type="text" v-model="subfolderPattern"
                            placeholder="{camera_model}" />
                    </div>
                </Transition>

                <!-- Placeholders (Collapsible) -->
                <div class="section collapsible">
                    <button class="section-toggle" @click="showPlaceholders = !showPlaceholders">
                        <span class="section-label">Verfügbare Platzhalter</span>
                        <span class="toggle-icon small">{{ showPlaceholders ? '▼' : '▶' }}</span>
                    </button>
                    <Transition name="slide">
                        <div v-if="showPlaceholders" class="placeholder-grid">
                            <code v-for="ph in placeholders" :key="ph" @click="insertPlaceholder(ph)">{{ ph }}</code>
                        </div>
                    </Transition>
                </div>

                <!-- Action Buttons -->
                <div class="actions">
                    <button @click="handlePreview" :disabled="!canPreview || loading" class="btn btn-secondary">
                        <span class="btn-icon">{{ loading ? '⏳' : '👁️' }}</span>
                        Preview
                    </button>
                    <button @click="handleExecute" :disabled="!canExecute || loading" class="btn btn-primary">
                        <span class="btn-icon">{{ loading ? '⏳' : '🚀' }}</span>
                        Umbenennen
                        <span v-if="stats.toRename > 0" class="btn-badge">{{ stats.toRename }}</span>
                    </button>
                </div>

                <!-- Preview Section -->
                <div v-if="previewItems && previewItems.length > 0" class="preview-section">
                    <div class="preview-header">
                        <span class="preview-title">Preview</span>
                        <div class="stats">
                            <span class="stat info">{{ stats.selected }}/{{ stats.total }}</span>
                            <span v-if="stats.toRename > 0" class="stat success">{{ stats.toRename }} ändern</span>
                            <span v-if="stats.conflicts > 0" class="stat error">{{ stats.conflicts }} Konflikte</span>
                        </div>
                    </div>

                    <div class="preview-list">
                        <div v-for="item in previewItems" :key="item.fullPath" :class="[
                            'preview-item',
                            {
                                conflict: item.hasConflict,
                                unchanged: item.originalName === item.newName,
                                unselected: !item.isSelected
                            }
                        ]">
                            <label class="item-checkbox">
                                <input type="checkbox" :checked="item.isSelected"
                                    @change="$emit('toggle-item', item)" />
                            </label>
                            <span v-if="item.dayNumber" class="day-badge">
                                Tag {{ item.dayNumber }}
                            </span>
                            <span v-else class="day-badge empty">—</span>
                            <div class="item-names">
                                <span class="item-original">{{ item.originalName }}</span>
                                <span class="item-arrow">→</span>
                                <span :class="['item-new', { changed: item.originalName !== item.newName }]">
                                    {{ item.newName }}
                                </span>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Info Box -->
                <div class="info-box">
                    <div class="info-header">
                        <span class="info-icon">ℹ️</span>
                        <span>So funktioniert's</span>
                    </div>
                    <ul>
                        <li>Fotos werden nach EXIF-Datum sortiert</li>
                        <li>GPS-Daten → Zeitzone → UTC-Konvertierung</li>
                        <li>Jeder Tag bekommt einen eigenen Ordner</li>
                        <li>Fotos ohne EXIF-Datum werden übersprungen</li>
                    </ul>
                </div>
            </div>
        </Transition>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import type { VacationModeOptions, RenamePreviewItem, FileInfo } from '../types';

interface Props {
    filePattern: string;
    previewItems?: RenamePreviewItem[];
    loading?: boolean;
    files?: FileInfo[];
    isActive?: boolean;
}

interface Emits {
    (e: 'preview', options: { pattern: string; vacationMode: VacationModeOptions }): void;
    (e: 'execute'): void;
    (e: 'toggle-item', item: RenamePreviewItem): void;
    (e: 'mode-change', active: boolean): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

const startDate = ref('');
const dayFolderPattern = ref('Tag {day_number}');
const subfolderPattern = ref('');
const useSubfolders = ref(false);
const localFilePattern = ref('{year}-{month}-{day}_{day_counter}');
const showPlaceholders = ref(false);

const placeholders = [
    '{day_number}', '{day_counter}', '{global_counter}',
    '{year}', '{month}', '{day}', '{date_taken}', '{time_taken}',
    '{filename}', '{camera_make}', '{camera_model}', '{counter:N}'
];

const canPreview = computed(() =>
    localFilePattern.value.trim() !== '' && dayFolderPattern.value.trim() !== ''
);

const stats = computed(() => {
    const items = props.previewItems || [];
    const total = items.length;
    const selected = items.filter(i => i.isSelected).length;
    const conflicts = items.filter(i => i.hasConflict).length;
    const toRename = items.filter(i => i.isSelected && !i.hasConflict && i.originalName !== i.newName).length;
    return { total, selected, conflicts, toRename };
});

const canExecute = computed(() => stats.value.toRename > 0 && !props.loading);

function toggleMode() {
    emit('mode-change', !props.isActive);
}

function onSubfolderToggle() {
    if (!useSubfolders.value) {
        subfolderPattern.value = '';
    }
}

function handlePreview() {
    if (!canPreview.value) return;

    const options: VacationModeOptions = {
        enabled: true,
        dayFolderPattern: dayFolderPattern.value || 'Tag {day_number}',
        startDate: startDate.value || undefined,
        subfolderPattern: useSubfolders.value ? subfolderPattern.value : undefined,
    };

    emit('preview', { pattern: localFilePattern.value, vacationMode: options });
}

function handleExecute() {
    if (!canExecute.value) return;
    emit('execute');
}

function insertPlaceholder(ph: string) {
    localFilePattern.value += ph;
}

// Auto-set start date from oldest photo (UTC) - fallback to createdAt if no EXIF date
watch(() => props.files, (newFiles) => {
    if (!newFiles || newFiles.length === 0) return;

    // Get dates from files - prefer dateTakenUtc, fallback to createdAt
    const filesWithDates = newFiles
        .map(f => ({
            file: f,
            dateUtc: f.dateTakenUtc
                ? new Date(f.dateTakenUtc)
                : new Date(f.createdAt)
        }))
        .filter(f => !isNaN(f.dateUtc.getTime()))
        .sort((a, b) => a.dateUtc.getTime() - b.dateUtc.getTime());

    const oldestFile = filesWithDates[0];
    if (oldestFile) {
        // Format as YYYY-MM-DD for the date input (from UTC date)
        const formattedDate = oldestFile.dateUtc.toISOString().split('T')[0];
        if (formattedDate) {
            startDate.value = formattedDate;
        }
    }
}, { immediate: true });
</script>

<style scoped>
.mode-panel {
    background: var(--bg-card, #22242e);
    border: 1px solid var(--border-color, #2e313d);
    border-radius: var(--radius-md, 10px);
    overflow: hidden;
    transition: all var(--transition-normal, 250ms ease);
}

.mode-panel.active {
    border-color: var(--vacation-border, rgba(245, 158, 11, 0.3));
    box-shadow: 0 0 0 1px var(--vacation-border, rgba(245, 158, 11, 0.3));
}

.panel-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 1rem 1.25rem;
    cursor: pointer;
    transition: background var(--transition-fast, 150ms ease);
    user-select: none;
}

.panel-header:hover {
    background: var(--bg-hover, #2a2d3a);
}

.mode-panel.active .panel-header {
    background: var(--vacation-bg, rgba(245, 158, 11, 0.12));
    border-bottom: 1px solid var(--border-color, #2e313d);
}

.header-left {
    display: flex;
    align-items: center;
    gap: 0.75rem;
}

.mode-icon {
    font-size: 1.25rem;
}

.panel-header h3 {
    margin: 0;
    font-size: 1rem;
    font-weight: 600;
    color: var(--text-color, #e8eaed);
}

.header-right {
    display: flex;
    align-items: center;
    gap: 0.75rem;
}

.badge {
    background: var(--vacation-color, #f59e0b);
    color: white;
    font-size: 0.75rem;
    font-weight: 600;
    padding: 0.2rem 0.5rem;
    border-radius: 10px;
    min-width: 1.5rem;
    text-align: center;
}

.toggle-icon {
    font-size: 0.7rem;
    color: var(--text-muted, #6b7280);
}

.toggle-icon.small {
    font-size: 0.6rem;
}

.panel-content {
    padding: 1.25rem;
    display: flex;
    flex-direction: column;
    gap: 1.25rem;
}

.config-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 1rem;
}

.form-group {
    display: flex;
    flex-direction: column;
    gap: 0.375rem;
}

.form-group.full-width {
    grid-column: 1 / -1;
}

.form-group label {
    font-size: 0.8rem;
    font-weight: 500;
    color: var(--text-muted, #6b7280);
    text-transform: uppercase;
    letter-spacing: 0.05em;
}

.form-group input {
    padding: 0.6rem 0.75rem;
    background: var(--bg-tertiary, #1f2028);
    border: 1px solid var(--border-color, #2e313d);
    border-radius: var(--radius-sm, 6px);
    color: var(--text-color, #e8eaed);
    font-size: 0.875rem;
    transition: border-color var(--transition-fast, 150ms ease);
}

.form-group input:focus {
    outline: none;
    border-color: var(--vacation-color, #f59e0b);
}

.form-group input::placeholder {
    color: var(--text-dim, #4b5563);
}

.form-group input[type="date"] {
    font-family: inherit;
}

.hint {
    font-size: 0.7rem;
    color: var(--text-dim, #4b5563);
}

.toggle-row {
    display: flex;
    align-items: center;
    gap: 0.75rem;
}

.toggle-switch {
    position: relative;
    width: 40px;
    height: 22px;
}

.toggle-switch input {
    opacity: 0;
    width: 0;
    height: 0;
}

.slider {
    position: absolute;
    cursor: pointer;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background: var(--bg-tertiary, #1f2028);
    border: 1px solid var(--border-color, #2e313d);
    border-radius: 11px;
    transition: all var(--transition-fast, 150ms ease);
}

.slider::before {
    position: absolute;
    content: "";
    height: 16px;
    width: 16px;
    left: 2px;
    bottom: 2px;
    background: var(--text-muted, #6b7280);
    border-radius: 50%;
    transition: all var(--transition-fast, 150ms ease);
}

.toggle-switch input:checked+.slider {
    background: var(--vacation-bg, rgba(245, 158, 11, 0.12));
    border-color: var(--vacation-color, #f59e0b);
}

.toggle-switch input:checked+.slider::before {
    background: var(--vacation-color, #f59e0b);
    transform: translateX(18px);
}

.toggle-label {
    font-size: 0.875rem;
    color: var(--text-secondary, #b8bcc4);
}

.subfolder-input {
    margin-left: 3rem;
}

.section.collapsible {
    background: var(--bg-tertiary, #1f2028);
    border-radius: var(--radius-sm, 6px);
    padding: 0.75rem;
}

.section-toggle {
    display: flex;
    justify-content: space-between;
    align-items: center;
    width: 100%;
    background: none;
    border: none;
    cursor: pointer;
    padding: 0;
}

.section-label {
    font-size: 0.8rem;
    font-weight: 500;
    color: var(--text-muted, #6b7280);
    text-transform: uppercase;
    letter-spacing: 0.05em;
}

.placeholder-grid {
    display: flex;
    flex-wrap: wrap;
    gap: 0.375rem;
    margin-top: 0.5rem;
}

.placeholder-grid code {
    padding: 0.25rem 0.5rem;
    background: var(--bg-card, #22242e);
    border: 1px solid var(--border-color, #2e313d);
    border-radius: 4px;
    font-size: 0.75rem;
    color: var(--vacation-color, #f59e0b);
    cursor: pointer;
    transition: all var(--transition-fast, 150ms ease);
}

.placeholder-grid code:hover {
    background: var(--vacation-bg, rgba(245, 158, 11, 0.12));
    border-color: var(--vacation-color, #f59e0b);
}

.actions {
    display: flex;
    gap: 0.75rem;
}

.btn {
    flex: 1;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 0.5rem;
    padding: 0.65rem 1rem;
    border: none;
    border-radius: var(--radius-sm, 6px);
    font-size: 0.875rem;
    font-weight: 500;
    cursor: pointer;
    transition: all var(--transition-fast, 150ms ease);
}

.btn-icon {
    font-size: 1rem;
}

.btn-badge {
    background: rgba(255, 255, 255, 0.2);
    padding: 0.1rem 0.4rem;
    border-radius: 8px;
    font-size: 0.75rem;
}

.btn-secondary {
    background: var(--bg-tertiary, #1f2028);
    color: var(--text-color, #e8eaed);
    border: 1px solid var(--border-color, #2e313d);
}

.btn-secondary:hover:not(:disabled) {
    background: var(--bg-hover, #2a2d3a);
    border-color: var(--text-muted, #6b7280);
}

.btn-primary {
    background: var(--vacation-color, #f59e0b);
    color: white;
}

.btn-primary:hover:not(:disabled) {
    background: #fbbf24;
    transform: translateY(-1px);
    box-shadow: 0 4px 12px rgba(245, 158, 11, 0.3);
}

.btn:disabled {
    opacity: 0.4;
    cursor: not-allowed;
    transform: none;
}

.preview-section {
    background: var(--bg-tertiary, #1f2028);
    border-radius: var(--radius-sm, 6px);
    overflow: hidden;
}

.preview-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 0.75rem 1rem;
    background: var(--bg-secondary, #17181c);
    border-bottom: 1px solid var(--border-color, #2e313d);
}

.preview-title {
    font-size: 0.85rem;
    font-weight: 600;
    color: var(--text-color, #e8eaed);
}

.stats {
    display: flex;
    gap: 0.5rem;
}

.stat {
    font-size: 0.7rem;
    font-weight: 500;
    padding: 0.2rem 0.5rem;
    border-radius: 8px;
}

.stat.info {
    background: var(--accent-bg, rgba(74, 144, 217, 0.12));
    color: var(--accent-color, #4a90d9);
}

.stat.success {
    background: var(--success-bg, rgba(52, 211, 153, 0.12));
    color: var(--success-color, #34d399);
}

.stat.error {
    background: var(--error-bg, rgba(248, 113, 113, 0.12));
    color: var(--error-color, #f87171);
}

.preview-list {
    max-height: 280px;
    overflow-y: auto;
}

.preview-item {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 0.6rem 1rem;
    border-bottom: 1px solid var(--border-light, #252830);
    transition: background var(--transition-fast, 150ms ease);
}

.preview-item:last-child {
    border-bottom: none;
}

.preview-item:hover {
    background: var(--bg-hover, #2a2d3a);
}

.preview-item.conflict {
    background: var(--error-bg, rgba(248, 113, 113, 0.12));
}

.preview-item.unchanged {
    opacity: 0.5;
}

.preview-item.unselected {
    opacity: 0.35;
}

.item-checkbox input {
    width: 14px;
    height: 14px;
    cursor: pointer;
}

.day-badge {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    min-width: 3.5rem;
    padding: 0.2rem 0.5rem;
    background: var(--vacation-bg, rgba(245, 158, 11, 0.12));
    border: 1px solid var(--vacation-border, rgba(245, 158, 11, 0.3));
    border-radius: 6px;
    font-size: 0.7rem;
    font-weight: 600;
    color: var(--vacation-color, #f59e0b);
}

.day-badge.empty {
    background: transparent;
    border-color: var(--border-color, #2e313d);
    color: var(--text-dim, #4b5563);
}

.item-names {
    flex: 1;
    display: flex;
    align-items: center;
    gap: 0.5rem;
    min-width: 0;
    font-family: 'JetBrains Mono', 'Consolas', monospace;
    font-size: 0.8rem;
}

.item-original {
    color: var(--text-muted, #6b7280);
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.item-arrow {
    color: var(--text-dim, #4b5563);
    flex-shrink: 0;
}

.item-new {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    color: var(--text-secondary, #b8bcc4);
}

.item-new.changed {
    color: var(--vacation-color, #f59e0b);
}

.info-box {
    background: var(--bg-tertiary, #1f2028);
    border: 1px solid var(--border-color, #2e313d);
    border-radius: var(--radius-sm, 6px);
    padding: 1rem;
}

.info-header {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-size: 0.85rem;
    font-weight: 600;
    color: var(--text-color, #e8eaed);
    margin-bottom: 0.75rem;
}

.info-icon {
    font-size: 1rem;
}

.info-box ul {
    margin: 0;
    padding-left: 1.25rem;
    font-size: 0.8rem;
    color: var(--text-secondary, #b8bcc4);
}

.info-box li {
    margin-bottom: 0.25rem;
}

/* Slide transition */
.slide-enter-active,
.slide-leave-active {
    transition: all var(--transition-normal, 250ms ease);
    overflow: hidden;
}

.slide-enter-from,
.slide-leave-to {
    opacity: 0;
    max-height: 0;
}

.slide-enter-to,
.slide-leave-from {
    opacity: 1;
    max-height: 1500px;
}

/* Scrollbar */
.preview-list::-webkit-scrollbar {
    width: 6px;
}

.preview-list::-webkit-scrollbar-track {
    background: transparent;
}

.preview-list::-webkit-scrollbar-thumb {
    background: var(--border-color, #2e313d);
    border-radius: 3px;
}

.preview-list::-webkit-scrollbar-thumb:hover {
    background: var(--text-muted, #6b7280);
}
</style>
