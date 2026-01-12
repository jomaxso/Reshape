<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import type { RenamePattern, RenamePreviewItem } from '../types';

const props = defineProps<{
    patterns: RenamePattern[];
    previewItems: RenamePreviewItem[];
    loading?: boolean;
}>();

const emit = defineEmits<{
    'preview': [pattern: string];
    'execute': [];
}>();

const selectedPattern = ref('');
const customPattern = ref('');

const activePattern = computed(() => {
    return customPattern.value || selectedPattern.value;
});

const stats = computed(() => {
    const total = props.previewItems.length;
    const conflicts = props.previewItems.filter(i => i.hasConflict).length;
    const unchanged = props.previewItems.filter(i => i.originalName === i.newName).length;
    const toRename = total - conflicts - unchanged;

    return { total, conflicts, unchanged, toRename };
});

function handlePatternSelect(pattern: string) {
    selectedPattern.value = pattern;
    customPattern.value = '';
    emit('preview', pattern);
}

function handleCustomPreview() {
    if (customPattern.value.trim()) {
        selectedPattern.value = '';
        emit('preview', customPattern.value.trim());
    }
}

function handleExecute() {
    if (stats.value.toRename > 0) {
        emit('execute');
    }
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

<template>
    <div class="rename-preview">
        <!-- Pattern Selection -->
        <div class="pattern-section">
            <h3>📝 Rename Pattern</h3>

            <div class="pattern-templates">
                <button v-for="pattern in patterns" :key="pattern.pattern"
                    :class="['pattern-btn', { active: selectedPattern === pattern.pattern && !customPattern }]"
                    :title="pattern.description" @click="handlePatternSelect(pattern.pattern)" :disabled="loading">
                    {{ pattern.pattern }}
                </button>
            </div>

            <div class="custom-pattern">
                <input v-model="customPattern" type="text"
                    placeholder="Or enter custom pattern: {year}-{month}-{day}_{filename}" :disabled="loading"
                    @keyup.enter="handleCustomPreview" />
                <button @click="handleCustomPreview" :disabled="!customPattern.trim() || loading" class="preview-btn">
                    Preview
                </button>
            </div>

            <div class="placeholders">
                <span class="label">Available:</span>
                <code>{filename}</code>
                <code>{ext}</code>
                <code>{year}</code>
                <code>{month}</code>
                <code>{day}</code>
                <code>{date_taken}</code>
                <code>{counter:N}</code>
            </div>
        </div>

        <!-- Preview Table -->
        <div v-if="previewItems.length > 0" class="preview-section">
            <div class="preview-header">
                <h3>👀 Preview</h3>
                <div class="stats">
                    <span class="stat rename">{{ stats.toRename }} to rename</span>
                    <span v-if="stats.conflicts > 0" class="stat conflict">{{ stats.conflicts }} conflicts</span>
                    <span v-if="stats.unchanged > 0" class="stat unchanged">{{ stats.unchanged }} unchanged</span>
                </div>
            </div>

            <div class="preview-table">
                <div class="table-header">
                    <span class="col-status"></span>
                    <span class="col-original">Original</span>
                    <span class="col-arrow"></span>
                    <span class="col-new">New Name</span>
                </div>

                <div class="table-body">
                    <div v-for="item in previewItems" :key="item.fullPath" :class="[
                        'preview-row',
                        {
                            conflict: item.hasConflict,
                            unchanged: item.originalName === item.newName
                        }
                    ]">
                        <span class="col-status">
                            <span v-if="item.hasConflict" class="status-icon conflict">⚠️</span>
                            <span v-else-if="item.originalName === item.newName" class="status-icon unchanged">➖</span>
                            <span v-else class="status-icon ok">✅</span>
                        </span>
                        <span class="col-original">{{ item.originalName }}</span>
                        <span class="col-arrow">→</span>
                        <span class="col-new" :class="{ highlight: item.originalName !== item.newName }">
                            {{ item.newName }}
                        </span>
                    </div>
                </div>
            </div>

            <!-- Execute Button -->
            <div class="actions">
                <button @click="handleExecute" :disabled="stats.toRename === 0 || loading" class="execute-btn">
                    <span v-if="loading" class="spinner"></span>
                    <span v-else>🚀 Rename {{ stats.toRename }} File(s)</span>
                </button>
            </div>
        </div>

        <!-- Empty State -->
        <div v-else-if="activePattern" class="empty-state">
            <span class="spinner large"></span>
            <p>Generating preview...</p>
        </div>
    </div>
</template>

<style scoped>
.rename-preview {
    display: flex;
    flex-direction: column;
    gap: 1.5rem;
}

.pattern-section,
.preview-section {
    background: var(--bg-secondary, #1e1e1e);
    border: 1px solid var(--border-color, #3e3e3e);
    border-radius: 8px;
    padding: 1.25rem;
}

h3 {
    margin: 0 0 1rem 0;
    font-size: 1rem;
    color: var(--text-color, #fff);
}

.pattern-templates {
    display: flex;
    flex-wrap: wrap;
    gap: 0.5rem;
    margin-bottom: 1rem;
}

.pattern-btn {
    padding: 0.5rem 0.75rem;
    background: var(--bg-tertiary, #252526);
    border: 1px solid var(--border-color, #3e3e3e);
    border-radius: 6px;
    color: var(--text-color, #fff);
    font-family: 'Consolas', 'Monaco', monospace;
    font-size: 0.85rem;
    cursor: pointer;
    transition: all 0.2s;
}

.pattern-btn:hover:not(:disabled) {
    border-color: var(--accent-color, #007acc);
    background: var(--bg-hover, #2a2a2a);
}

.pattern-btn.active {
    background: var(--accent-color, #007acc);
    border-color: var(--accent-color, #007acc);
}

.pattern-btn:disabled {
    opacity: 0.5;
    cursor: not-allowed;
}

.custom-pattern {
    display: flex;
    gap: 0.5rem;
    margin-bottom: 1rem;
}

.custom-pattern input {
    flex: 1;
    padding: 0.625rem 1rem;
    background: var(--bg-tertiary, #252526);
    border: 1px solid var(--border-color, #3e3e3e);
    border-radius: 6px;
    color: var(--text-color, #fff);
    font-family: 'Consolas', 'Monaco', monospace;
}

.custom-pattern input:focus {
    outline: none;
    border-color: var(--accent-color, #007acc);
}

.preview-btn {
    padding: 0.625rem 1rem;
    background: var(--bg-tertiary, #252526);
    border: 1px solid var(--border-color, #3e3e3e);
    border-radius: 6px;
    color: var(--text-color, #fff);
    cursor: pointer;
    transition: all 0.2s;
}

.preview-btn:hover:not(:disabled) {
    background: var(--accent-color, #007acc);
    border-color: var(--accent-color, #007acc);
}

.preview-btn:disabled {
    opacity: 0.5;
    cursor: not-allowed;
}

.placeholders {
    display: flex;
    flex-wrap: wrap;
    gap: 0.5rem;
    align-items: center;
    font-size: 0.8rem;
}

.placeholders .label {
    color: var(--text-muted, #888);
}

.placeholders code {
    padding: 0.2rem 0.5rem;
    background: var(--bg-tertiary, #252526);
    border-radius: 4px;
    color: var(--accent-color, #007acc);
}

.preview-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 1rem;
}

.preview-header h3 {
    margin: 0;
}

.stats {
    display: flex;
    gap: 1rem;
}

.stat {
    font-size: 0.85rem;
    padding: 0.25rem 0.75rem;
    border-radius: 12px;
}

.stat.rename {
    background: rgba(76, 175, 80, 0.2);
    color: #4caf50;
}

.stat.conflict {
    background: rgba(244, 67, 54, 0.2);
    color: #f44336;
}

.stat.unchanged {
    background: rgba(158, 158, 158, 0.2);
    color: #9e9e9e;
}

.preview-table {
    border: 1px solid var(--border-color, #3e3e3e);
    border-radius: 6px;
    overflow: hidden;
}

.table-header {
    display: grid;
    grid-template-columns: 40px 1fr 30px 1fr;
    gap: 0.5rem;
    padding: 0.75rem 1rem;
    background: var(--bg-tertiary, #252526);
    font-weight: 600;
    font-size: 0.8rem;
    color: var(--text-muted, #888);
    text-transform: uppercase;
}

.table-body {
    max-height: 300px;
    overflow-y: auto;
}

.preview-row {
    display: grid;
    grid-template-columns: 40px 1fr 30px 1fr;
    gap: 0.5rem;
    padding: 0.5rem 1rem;
    border-bottom: 1px solid var(--border-light, #2d2d2d);
    font-family: 'Consolas', 'Monaco', monospace;
    font-size: 0.9rem;
}

.preview-row.conflict {
    background: rgba(244, 67, 54, 0.1);
}

.preview-row.unchanged {
    opacity: 0.5;
}

.col-status {
    text-align: center;
}

.col-arrow {
    text-align: center;
    color: var(--text-muted, #888);
}

.col-original {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.col-new {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.col-new.highlight {
    color: var(--accent-color, #007acc);
}

.actions {
    margin-top: 1rem;
    text-align: right;
}

.execute-btn {
    padding: 0.75rem 1.5rem;
    background: linear-gradient(135deg, #4caf50, #45a049);
    border: none;
    border-radius: 8px;
    color: white;
    font-size: 1rem;
    font-weight: 600;
    cursor: pointer;
    transition: all 0.2s;
    display: inline-flex;
    align-items: center;
    gap: 0.5rem;
}

.execute-btn:hover:not(:disabled) {
    transform: translateY(-2px);
    box-shadow: 0 4px 12px rgba(76, 175, 80, 0.3);
}

.execute-btn:disabled {
    opacity: 0.5;
    cursor: not-allowed;
    transform: none;
}

.spinner {
    width: 16px;
    height: 16px;
    border: 2px solid rgba(255, 255, 255, 0.3);
    border-top-color: white;
    border-radius: 50%;
    animation: spin 0.8s linear infinite;
}

.spinner.large {
    width: 32px;
    height: 32px;
    border-width: 3px;
}

@keyframes spin {
    to {
        transform: rotate(360deg);
    }
}

.empty-state {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 3rem;
    color: var(--text-muted, #888);
}
</style>
