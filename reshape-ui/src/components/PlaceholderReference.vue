<template>
    <div class="placeholder-reference">
        <div class="reference-header" @click="isExpanded = !isExpanded">
            <div class="header-left">
                <span class="ref-icon">📖</span>
                <span class="ref-title">Platzhalter-Referenz</span>
            </div>
            <span class="toggle-icon">{{ isExpanded ? '▼' : '▶' }}</span>
        </div>

        <Transition name="slide">
            <div v-if="isExpanded" class="reference-content">
                <div class="category" v-for="category in categories" :key="category.name">
                    <h4 class="category-title">{{ category.name }}</h4>
                    <div class="placeholder-list">
                        <div v-for="ph in category.items" :key="ph.key" class="placeholder-row"
                            @click="$emit('insert', ph.key)">
                            <code class="ph-key">{{ ph.key }}</code>
                            <span class="ph-desc">{{ ph.desc }}</span>
                            <span v-if="ph.example" class="ph-example">{{ ph.example }}</span>
                        </div>
                    </div>
                </div>
            </div>
        </Transition>
    </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';

defineEmits<{
    (e: 'insert', key: string): void;
}>();

const isExpanded = ref(false);

const categories = [
    {
        name: '📅 Datum & Zeit',
        items: [
            { key: '{year}', desc: 'Jahr (4-stellig)', example: '→ 2024' },
            { key: '{month}', desc: 'Monat (2-stellig)', example: '→ 01' },
            { key: '{day}', desc: 'Tag (2-stellig)', example: '→ 15' },
            { key: '{date_taken}', desc: 'EXIF-Aufnahmedatum', example: '→ 2024-01-15' },
            { key: '{time_taken}', desc: 'EXIF-Aufnahmezeit', example: '→ 14-30-00' },
            { key: '{created}', desc: 'Erstellungsdatum der Datei', example: '→ 2024-01-15' },
            { key: '{created_time}', desc: 'Erstellungszeit der Datei', example: '→ 14-30-00' },
            { key: '{modified}', desc: 'Änderungsdatum der Datei', example: '→ 2024-01-15' },
            { key: '{modified_time}', desc: 'Änderungszeit der Datei', example: '→ 14-30-00' },
        ]
    },
    {
        name: '📁 Datei',
        items: [
            { key: '{filename}', desc: 'Originaler Dateiname ohne Erweiterung', example: '→ IMG_1234' },
            { key: '{ext}', desc: 'Dateierweiterung', example: '→ jpg' },
            { key: '{size}', desc: 'Dateigröße in Bytes', example: '→ 2048576' },
        ]
    },
    {
        name: '📷 Kamera & Bild',
        items: [
            { key: '{camera_make}', desc: 'Kamerahersteller', example: '→ Apple' },
            { key: '{camera_model}', desc: 'Kameramodell', example: '→ iPhone 15 Pro' },
            { key: '{width}', desc: 'Bildbreite in Pixel', example: '→ 4032' },
            { key: '{height}', desc: 'Bildhöhe in Pixel', example: '→ 3024' },
        ]
    },
    {
        name: '📍 GPS',
        items: [
            { key: '{gps_lat}', desc: 'GPS-Breitengrad', example: '→ 52.520008' },
            { key: '{gps_lon}', desc: 'GPS-Längengrad', example: '→ 13.404954' },
        ]
    },
    {
        name: '🔢 Zähler',
        items: [
            { key: '{counter:N}', desc: 'Zähler mit N Stellen', example: '{counter:3} → 001' },
            { key: '{day_counter}', desc: 'Zähler pro Tag (Urlaubsmodus)', example: '→ 001' },
            { key: '{day_number}', desc: 'Urlaubstag-Nummer', example: '→ 1, 2, 3...' },
            { key: '{global_counter}', desc: 'Globaler Zähler (Urlaubsmodus)', example: '→ 0001' },
        ]
    },
];
</script>

<style scoped>
.placeholder-reference {
    background: var(--bg-card, #22242e);
    border: 1px solid var(--border-color, #2e313d);
    border-radius: var(--radius-md, 10px);
    overflow: hidden;
}

.reference-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 0.875rem 1rem;
    cursor: pointer;
    transition: background var(--transition-fast, 150ms ease);
    user-select: none;
}

.reference-header:hover {
    background: var(--bg-hover, #2a2d3a);
}

.header-left {
    display: flex;
    align-items: center;
    gap: 0.5rem;
}

.ref-icon {
    font-size: 1rem;
}

.ref-title {
    font-size: 0.875rem;
    font-weight: 600;
    color: var(--text-color, #e8eaed);
}

.toggle-icon {
    font-size: 0.75rem;
    color: var(--text-muted, #6b7280);
}

.reference-content {
    padding: 0.5rem 1rem 1rem;
    border-top: 1px solid var(--border-color, #2e313d);
    max-height: 400px;
    overflow-y: auto;
}

.category {
    margin-bottom: 1rem;
}

.category:last-child {
    margin-bottom: 0;
}

.category-title {
    font-size: 0.75rem;
    font-weight: 600;
    color: var(--accent-color, #4a90d9);
    margin-bottom: 0.5rem;
    padding-bottom: 0.25rem;
    border-bottom: 1px solid var(--border-color, #2e313d);
}

.placeholder-list {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
}

.placeholder-row {
    display: grid;
    grid-template-columns: 120px 1fr auto;
    align-items: center;
    gap: 0.75rem;
    padding: 0.375rem 0.5rem;
    border-radius: 4px;
    cursor: pointer;
    transition: background var(--transition-fast, 150ms ease);
}

.placeholder-row:hover {
    background: var(--bg-hover, #2a2d3a);
}

.ph-key {
    font-size: 0.75rem;
    color: var(--accent-color, #4a90d9);
    font-family: 'JetBrains Mono', 'Consolas', monospace;
}

.ph-desc {
    font-size: 0.75rem;
    color: var(--text-secondary, #b8bcc4);
}

.ph-example {
    font-size: 0.65rem;
    color: var(--text-muted, #6b7280);
    font-family: 'JetBrains Mono', 'Consolas', monospace;
    white-space: nowrap;
}

/* Slide transition */
.slide-enter-active,
.slide-leave-active {
    transition: all 0.25s ease;
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
    max-height: 500px;
}
</style>
