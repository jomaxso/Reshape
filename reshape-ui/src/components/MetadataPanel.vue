<script setup lang="ts">
import type { FileInfo } from '../types';

defineProps<{
    file: FileInfo;
}>();
</script>

<template>
    <div class="metadata-panel">
        <h3>📋 File Details</h3>

        <div class="file-name">
            <strong>{{ file.name }}</strong>
        </div>

        <div class="metadata-grid">
            <div v-for="(value, key) in file.metadata" :key="key" class="metadata-item">
                <span class="key">{{ key }}</span>
                <span class="value">{{ value }}</span>
            </div>
        </div>

        <div class="path-info">
            <span class="label">Full Path:</span>
            <code class="path">{{ file.fullPath }}</code>
        </div>
    </div>
</template>

<style scoped>
.metadata-panel {
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

.file-name {
    padding: 0.75rem;
    background: var(--bg-tertiary, #252526);
    border-radius: 6px;
    margin-bottom: 1rem;
    font-family: 'Consolas', 'Monaco', monospace;
    word-break: break-all;
}

.metadata-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
    gap: 0.75rem;
    margin-bottom: 1rem;
}

.metadata-item {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
    padding: 0.5rem;
    background: var(--bg-tertiary, #252526);
    border-radius: 4px;
}

.key {
    font-size: 0.75rem;
    color: var(--accent-color, #007acc);
    text-transform: uppercase;
    letter-spacing: 0.5px;
}

.value {
    font-family: 'Consolas', 'Monaco', monospace;
    color: var(--text-color, #fff);
}

.path-info {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.label {
    font-size: 0.85rem;
    color: var(--text-muted, #888);
}

.path {
    font-size: 0.8rem;
    padding: 0.5rem;
    background: var(--bg-tertiary, #252526);
    border-radius: 4px;
    word-break: break-all;
    color: var(--text-muted, #aaa);
}
</style>
