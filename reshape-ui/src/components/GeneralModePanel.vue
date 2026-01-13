<template>
    <div :class="['mode-panel', 'general-mode', { active: isActive }]">
        <div class="panel-header" @click="toggleMode">
            <div class="header-left">
                <span class="mode-icon">⚙️</span>
                <h3>Allgemeiner Modus</h3>
            </div>
            <div class="header-right">
                <span v-if="stats.toRename > 0 && isActive" class="badge">{{ stats.toRename }}</span>
                <span class="toggle-icon">{{ isActive ? '▼' : '▶' }}</span>
            </div>
        </div>

        <Transition name="slide">
            <div v-if="isActive" class="panel-content">
                <!-- Pattern Templates -->
                <div class="section">
                    <label class="section-label">Vorlagen</label>
                    <div class="pattern-templates">
                        <button v-for="pattern in patterns" :key="pattern.pattern"
                            :class="['pattern-chip', { active: selectedPattern === pattern.pattern && !customPattern }]"
                            :title="pattern.description" @click="handlePatternSelect(pattern.pattern)">
                            {{ pattern.pattern }}
                        </button>
                    </div>
                </div>

                <!-- Custom Pattern Input -->
                <div class="section">
                    <label class="section-label">Eigenes Muster</label>
                    <div class="input-wrapper">
                        <input key="custom-pattern-input" v-model="customPattern" type="text"
                            placeholder="{year}-{month}-{day}_{filename}" />
                    </div>
                </div>

                <!-- Available Placeholders -->
                <div class="section collapsible">
                    <button class="section-toggle" @click="showPlaceholders = !showPlaceholders">
                        <span class="section-label">Verfügbare Platzhalter</span>
                        <span class="toggle-icon small">{{ showPlaceholders ? '▼' : '▶' }}</span>
                    </button>
                    <Transition name="slide">
                        <div v-if="showPlaceholders" class="placeholder-grid">
                            <div v-for="ph in placeholders" :key="ph.key" class="placeholder-item" :title="ph.desc"
                                @click="insertPlaceholder(ph)">
                                <code>{{ ph.key }}</code>
                                <span class="placeholder-desc">{{ ph.desc }}</span>
                            </div>
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
                            <span v-if="stats.unchanged > 0" class="stat muted">{{ stats.unchanged }} gleich</span>
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
                            <span class="item-status">
                                <span v-if="item.hasConflict" class="status-dot error" title="Konflikt">⚠️</span>
                                <span v-else-if="item.originalName === item.newName" class="status-dot muted"
                                    title="Unverändert">−</span>
                                <span v-else class="status-dot success" title="OK">✓</span>
                            </span>
                            <div class="item-names">
                                <span class="item-original">{{ item.originalName }}</span>
                                <span class="item-arrow">→</span>
                                <span :class="['item-new', { changed: item.originalName !== item.newName }]">
                                    {{ item.newName }}
                                </span>
                            </div>
                            <span v-if="item.relativePath" class="item-folder" :title="item.relativePath">
                                📁 {{ item.relativePath }}
                            </span>
                        </div>
                    </div>
                </div>
            </div>
        </Transition>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import type { RenamePattern, RenamePreviewItem } from '../types';

interface Props {
    patterns: RenamePattern[];
    previewItems?: RenamePreviewItem[];
    loading?: boolean;
    isActive?: boolean;
}

interface Emits {
    (e: 'preview', pattern: string): void;
    (e: 'execute'): void;
    (e: 'toggle-item', item: RenamePreviewItem): void;
    (e: 'mode-change', active: boolean): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

const selectedPattern = ref('');
const customPattern = ref('');
const showPlaceholders = ref(false);

const placeholders = [
    { key: '{filename}', desc: 'Originaler Dateiname ohne Erweiterung' },
    { key: '{ext}', desc: 'Dateierweiterung (z.B. jpg)' },
    { key: '{year}', desc: 'Jahr (4-stellig)' },
    { key: '{month}', desc: 'Monat (2-stellig)' },
    { key: '{day}', desc: 'Tag (2-stellig)' },
    { key: '{date_taken}', desc: 'EXIF-Aufnahmedatum (YYYY-MM-DD)' },
    { key: '{time_taken}', desc: 'EXIF-Aufnahmezeit (HH-mm-ss)' },
    { key: '{camera_make}', desc: 'Kamerahersteller' },
    { key: '{camera_model}', desc: 'Kameramodell' },
    { key: '{width}', desc: 'Bildbreite in Pixel' },
    { key: '{height}', desc: 'Bildhöhe in Pixel' },
    { key: '{counter:N}', desc: 'Zähler mit N Stellen (z.B. {counter:3} → 001)' },
    { key: '{gps_lat}', desc: 'GPS-Breitengrad' },
    { key: '{gps_lon}', desc: 'GPS-Längengrad' },
    { key: '{created}', desc: 'Erstellungsdatum der Datei' },
    { key: '{modified}', desc: 'Änderungsdatum der Datei' },
];

const activePattern = computed(() => customPattern.value || selectedPattern.value);

const canPreview = computed(() => activePattern.value.trim() !== '');

const stats = computed(() => {
    const items = props.previewItems || [];
    const total = items.length;
    const selected = items.filter(i => i.isSelected).length;
    const conflicts = items.filter(i => i.hasConflict).length;
    const unchanged = items.filter(i => i.originalName === i.newName).length;
    const toRename = items.filter(i => i.isSelected && !i.hasConflict && i.originalName !== i.newName).length;
    return { total, selected, conflicts, unchanged, toRename };
});

const canExecute = computed(() => stats.value.toRename > 0 && !props.loading);

function toggleMode() {
    emit('mode-change', !props.isActive);
}

function handlePatternSelect(pattern: string) {
    selectedPattern.value = pattern;
    customPattern.value = '';
    emit('preview', pattern);
}

function handlePreview() {
    if (!canPreview.value) return;
    emit('preview', activePattern.value);
}

function handleExecute() {
    if (!canExecute.value) return;
    emit('execute');
}

function insertPlaceholder(ph: { key: string; desc: string }) {
    customPattern.value += ph.key;
}

// Debounced custom pattern preview
let debounceTimer: number | null = null;
watch(customPattern, (val) => {
    if (debounceTimer) clearTimeout(debounceTimer);
    if (val.trim()) {
        debounceTimer = window.setTimeout(() => {
            selectedPattern.value = '';
            emit('preview', val.trim());
        }, 500);
    }
});
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
    border-color: var(--general-border, rgba(139, 92, 246, 0.3));
    box-shadow: 0 0 0 1px var(--general-border, rgba(139, 92, 246, 0.3));
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
    background: var(--general-bg, rgba(139, 92, 246, 0.12));
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
    background: var(--general-color, #8b5cf6);
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
    transition: transform var(--transition-fast, 150ms ease);
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

.section {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.section-label {
    font-size: 0.8rem;
    font-weight: 500;
    color: var(--text-muted, #6b7280);
    text-transform: uppercase;
    letter-spacing: 0.05em;
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

.pattern-templates {
    display: flex;
    flex-wrap: wrap;
    gap: 0.5rem;
}

.pattern-chip {
    padding: 0.4rem 0.75rem;
    background: var(--bg-tertiary, #1f2028);
    border: 1px solid var(--border-color, #2e313d);
    border-radius: var(--radius-sm, 6px);
    color: var(--text-secondary, #b8bcc4);
    font-family: 'JetBrains Mono', 'Consolas', monospace;
    font-size: 0.8rem;
    cursor: pointer;
    transition: all var(--transition-fast, 150ms ease);
}

.pattern-chip:hover:not(:disabled) {
    border-color: var(--general-color, #8b5cf6);
    color: var(--text-color, #e8eaed);
}

.pattern-chip.active {
    background: var(--general-bg, rgba(139, 92, 246, 0.12));
    border-color: var(--general-color, #8b5cf6);
    color: var(--general-color, #8b5cf6);
}

.pattern-chip:disabled {
    opacity: 0.5;
    cursor: not-allowed;
}

.input-wrapper input {
    width: 100%;
    padding: 0.65rem 0.875rem;
    background: var(--bg-tertiary, #1f2028);
    border: 1px solid var(--border-color, #2e313d);
    border-radius: var(--radius-sm, 6px);
    color: var(--text-color, #e8eaed);
    font-family: 'JetBrains Mono', 'Consolas', monospace;
    font-size: 0.875rem;
    transition: border-color var(--transition-fast, 150ms ease);
}

.input-wrapper input:focus {
    outline: none;
    border-color: var(--general-color, #8b5cf6);
}

.input-wrapper input::placeholder {
    color: var(--text-dim, #4b5563);
}

.placeholder-grid {
    display: flex;
    flex-direction: column;
    gap: 0.375rem;
    margin-top: 0.5rem;
}

.placeholder-item {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 0.375rem 0.5rem;
    background: var(--bg-card, #22242e);
    border: 1px solid var(--border-color, #2e313d);
    border-radius: 4px;
    cursor: pointer;
    transition: all var(--transition-fast, 150ms ease);
}

.placeholder-item:hover {
    background: var(--general-bg, rgba(139, 92, 246, 0.12));
    border-color: var(--general-color, #8b5cf6);
}

.placeholder-item code {
    font-size: 0.75rem;
    color: var(--general-color, #8b5cf6);
    min-width: 100px;
}

.placeholder-desc {
    font-size: 0.7rem;
    color: var(--text-muted, #6b7280);
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
    background: var(--general-color, #8b5cf6);
    color: white;
}

.btn-primary:hover:not(:disabled) {
    background: #9d6eff;
    transform: translateY(-1px);
    box-shadow: 0 4px 12px rgba(139, 92, 246, 0.3);
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

.stat.muted {
    background: rgba(107, 114, 128, 0.12);
    color: var(--text-muted, #6b7280);
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

.item-status {
    width: 1.25rem;
    text-align: center;
    font-size: 0.85rem;
}

.status-dot.success {
    color: var(--success-color, #34d399);
}

.status-dot.error {
    color: var(--error-color, #f87171);
}

.status-dot.muted {
    color: var(--text-muted, #6b7280);
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
    color: var(--general-color, #8b5cf6);
}

.item-folder {
    font-size: 0.7rem;
    color: var(--text-dim, #4b5563);
    white-space: nowrap;
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
    max-height: 1000px;
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
