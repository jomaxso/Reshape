<script setup lang="ts">
import type { FileInfo } from '../types';

defineProps<{
    file: FileInfo;
}>();
</script>

<template>
    <div class="metadata-panel">
        <div class="panel-header">
            <span class="header-icon">📋</span>
            <span class="header-title">Datei-Details</span>
        </div>

        <div class="file-name">
            {{ file.name }}
        </div>

        <div class="metadata-grid">
            <div v-for="(value, key) in file.metadata" :key="key" class="metadata-item">
                <span class="meta-key">{{ key }}</span>
                <span class="meta-value">{{ value }}</span>
            </div>
        </div>

        <div v-if="file.gpsCoordinates" class="gps-info">
            <span class="gps-icon">📍</span>
            <span class="gps-coords">
                {{ file.gpsCoordinates.latitude.toFixed(6) }}, {{ file.gpsCoordinates.longitude.toFixed(6) }}
            </span>
        </div>

        <div class="path-info">
            <span class="path-label">Vollständiger Pfad</span>
            <code class="path-value">{{ file.fullPath }}</code>
        </div>
    </div>
</template>

<style scoped>
.metadata-panel {
    background: var(--bg-card, #22242e);
    border: 1px solid var(--border-color, #2e313d);
    border-radius: var(--radius-md, 10px);
    overflow: hidden;
}

.panel-header {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.75rem 1rem;
    background: var(--bg-tertiary, #1f2028);
    border-bottom: 1px solid var(--border-color, #2e313d);
}

.header-icon {
    font-size: 1rem;
}

.header-title {
    font-size: 0.875rem;
    font-weight: 600;
    color: var(--text-color, #e8eaed);
}

.file-name {
    padding: 0.75rem 1rem;
    font-family: 'JetBrains Mono', 'Consolas', monospace;
    font-size: 0.9rem;
    font-weight: 500;
    color: var(--text-color, #e8eaed);
    background: var(--bg-tertiary, #1f2028);
    border-bottom: 1px solid var(--border-color, #2e313d);
    word-break: break-all;
}

.metadata-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(140px, 1fr));
    gap: 0.5rem;
    padding: 1rem;
}

.metadata-item {
    display: flex;
    flex-direction: column;
    gap: 0.2rem;
    padding: 0.5rem;
    background: var(--bg-tertiary, #1f2028);
    border-radius: var(--radius-sm, 6px);
}

.meta-key {
    font-size: 0.65rem;
    font-weight: 500;
    color: var(--accent-color, #4a90d9);
    text-transform: uppercase;
    letter-spacing: 0.05em;
}

.meta-value {
    font-family: 'JetBrains Mono', 'Consolas', monospace;
    font-size: 0.8rem;
    color: var(--text-color, #e8eaed);
}

.gps-info {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.75rem 1rem;
    background: var(--success-bg, rgba(52, 211, 153, 0.12));
    border-top: 1px solid var(--border-color, #2e313d);
}

.gps-icon {
    font-size: 1rem;
}

.gps-coords {
    font-family: 'JetBrains Mono', 'Consolas', monospace;
    font-size: 0.8rem;
    color: var(--success-color, #34d399);
}

.path-info {
    display: flex;
    flex-direction: column;
    gap: 0.375rem;
    padding: 0.75rem 1rem;
    border-top: 1px solid var(--border-color, #2e313d);
}

.path-label {
    font-size: 0.7rem;
    font-weight: 500;
    color: var(--text-muted, #6b7280);
    text-transform: uppercase;
    letter-spacing: 0.05em;
}

.path-value {
    font-size: 0.75rem;
    padding: 0.5rem;
    background: var(--bg-tertiary, #1f2028);
    border-radius: var(--radius-sm, 6px);
    word-break: break-all;
    color: var(--text-dim, #4b5563);
}
</style>
