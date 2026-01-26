<script setup lang="ts">
import { ref, computed } from 'vue';

const emit = defineEmits<{
    'update:selectedExtensions': [extensions: string[]];
}>();

// Available file extensions (matching CLI)
const availableExtensions = [
    { ext: '.jpg', label: 'JPG', category: 'image' },
    { ext: '.jpeg', label: 'JPEG', category: 'image' },
    { ext: '.png', label: 'PNG', category: 'image' },
    { ext: '.heic', label: 'HEIC', category: 'image' },
    { ext: '.gif', label: 'GIF', category: 'image' },
    { ext: '.bmp', label: 'BMP', category: 'image' },
    { ext: '.tiff', label: 'TIFF', category: 'image' },
    { ext: '.raw', label: 'RAW', category: 'image' },
    { ext: '.mp4', label: 'MP4', category: 'video' },
    { ext: '.mov', label: 'MOV', category: 'video' },
    { ext: '.avi', label: 'AVI', category: 'video' },
];

// Initially all extensions are selected (as per requirement)
const selectedExtensions = ref<Set<string>>(
    new Set(availableExtensions.map(e => e.ext))
);

const isExpanded = ref(false);

// Group extensions by category
const groupedExtensions = computed(() => {
    const groups: Record<string, typeof availableExtensions> = {
        image: [],
        video: []
    };
    
    availableExtensions.forEach(ext => {
        const group = groups[ext.category];
        if (group) {
            group.push(ext);
        }
    });
    
    return groups;
});

// Check if all extensions are selected
const allSelected = computed(() => 
    selectedExtensions.value.size === availableExtensions.length
);

// Count selected extensions
const selectedCount = computed(() => selectedExtensions.value.size);

function toggleExtension(ext: string) {
    if (selectedExtensions.value.has(ext)) {
        selectedExtensions.value.delete(ext);
    } else {
        selectedExtensions.value.add(ext);
    }
    // Trigger reactivity by creating new Set reference
    selectedExtensions.value = new Set(selectedExtensions.value);
    emitUpdate();
}

function toggleAll() {
    if (allSelected.value) {
        selectedExtensions.value.clear();
    } else {
        selectedExtensions.value = new Set(availableExtensions.map(e => e.ext));
    }
    emitUpdate();
}

function selectCategory(category: string) {
    const categoryExts = availableExtensions
        .filter(e => e.category === category)
        .map(e => e.ext);
    
    const allCategorySelected = categoryExts.every(ext => selectedExtensions.value.has(ext));
    
    if (allCategorySelected) {
        categoryExts.forEach(ext => selectedExtensions.value.delete(ext));
    } else {
        categoryExts.forEach(ext => selectedExtensions.value.add(ext));
    }
    
    // Trigger reactivity by creating new Set reference
    selectedExtensions.value = new Set(selectedExtensions.value);
    emitUpdate();
}

function isCategorySelected(category: string): boolean {
    const categoryExts = availableExtensions
        .filter(e => e.category === category)
        .map(e => e.ext);
    return categoryExts.every(ext => selectedExtensions.value.has(ext));
}

function emitUpdate() {
    emit('update:selectedExtensions', Array.from(selectedExtensions.value));
}

function getCategoryLabel(category: string): string {
    const labels: Record<string, string> = {
        image: '🖼️ Bilder',
        video: '🎬 Videos'
    };
    return labels[category] || category;
}
</script>

<template>
    <div class="extension-filter">
        <div class="filter-header" @click="isExpanded = !isExpanded">
            <div class="header-left">
                <span class="filter-icon">🔍</span>
                <span class="filter-title">Dateitypen</span>
                <span class="filter-count">{{ selectedCount }} / {{ availableExtensions.length }}</span>
            </div>
            <div class="header-right">
                <button class="toggle-all-btn" @click.stop="toggleAll" :title="allSelected ? 'Alle abwählen' : 'Alle auswählen'">
                    {{ allSelected ? '☐' : '☑' }} Alle
                </button>
                <span class="expand-icon">{{ isExpanded ? '▼' : '▶' }}</span>
            </div>
        </div>
        
        <Transition name="filter-expand">
            <div v-if="isExpanded" class="filter-content">
                <div v-for="(exts, category) in groupedExtensions" :key="category" class="category-group">
                    <div class="category-header" @click="selectCategory(category)">
                        <span class="category-icon">
                            <input type="checkbox" :checked="isCategorySelected(category)" />
                        </span>
                        <span class="category-label">{{ getCategoryLabel(category) }}</span>
                    </div>
                    <div class="extensions-grid">
                        <label 
                            v-for="ext in exts" 
                            :key="ext.ext" 
                            class="extension-item"
                            :class="{ selected: selectedExtensions.has(ext.ext) }"
                        >
                            <input 
                                type="checkbox" 
                                :checked="selectedExtensions.has(ext.ext)"
                                @change="toggleExtension(ext.ext)"
                            />
                            <span class="ext-label">{{ ext.label }}</span>
                        </label>
                    </div>
                </div>
            </div>
        </Transition>
    </div>
</template>

<style scoped>
.extension-filter {
    background: var(--bg-card, #22242e);
    border: 1px solid var(--border-color, #2e313d);
    border-radius: var(--radius-md, 10px);
    margin-bottom: 1.25rem;
    overflow: hidden;
}

.filter-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 0.75rem 1rem;
    cursor: pointer;
    transition: background var(--transition-fast, 150ms ease);
    user-select: none;
}

.filter-header:hover {
    background: var(--bg-hover, #2a2d3a);
}

.header-left {
    display: flex;
    align-items: center;
    gap: 0.625rem;
}

.filter-icon {
    font-size: 1rem;
}

.filter-title {
    font-size: 0.875rem;
    font-weight: 600;
    color: var(--text-color, #e8eaed);
}

.filter-count {
    font-size: 0.75rem;
    color: var(--text-dim, #4b5563);
    background: var(--bg-tertiary, #1f2028);
    padding: 0.2rem 0.5rem;
    border-radius: 4px;
}

.header-right {
    display: flex;
    align-items: center;
    gap: 0.75rem;
}

.toggle-all-btn {
    font-size: 0.75rem;
    padding: 0.25rem 0.5rem;
    background: var(--bg-tertiary, #1f2028);
    border: 1px solid var(--border-color, #2e313d);
    border-radius: var(--radius-sm, 6px);
    color: var(--text-secondary, #b8bcc4);
    cursor: pointer;
    transition: all var(--transition-fast, 150ms ease);
}

.toggle-all-btn:hover {
    background: var(--bg-hover, #2a2d3a);
    border-color: var(--accent-color, #4a90d9);
    color: var(--accent-color, #4a90d9);
}

.expand-icon {
    font-size: 0.65rem;
    color: var(--text-muted, #6b7280);
}

.filter-content {
    padding: 0 1rem 1rem 1rem;
    border-top: 1px solid var(--border-light, #252830);
}

.category-group {
    margin-top: 0.75rem;
}

.category-header {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.5rem 0;
    cursor: pointer;
    user-select: none;
}

.category-icon input[type="checkbox"] {
    width: 14px;
    height: 14px;
    cursor: pointer;
}

.category-label {
    font-size: 0.8rem;
    font-weight: 500;
    color: var(--text-secondary, #b8bcc4);
}

.extensions-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(80px, 1fr));
    gap: 0.5rem;
    margin-left: 1.5rem;
}

.extension-item {
    display: flex;
    align-items: center;
    gap: 0.375rem;
    padding: 0.375rem 0.5rem;
    background: var(--bg-tertiary, #1f2028);
    border: 1px solid var(--border-color, #2e313d);
    border-radius: var(--radius-sm, 6px);
    cursor: pointer;
    transition: all var(--transition-fast, 150ms ease);
    font-size: 0.75rem;
}

.extension-item:hover {
    background: var(--bg-hover, #2a2d3a);
    border-color: var(--accent-color, #4a90d9);
}

.extension-item.selected {
    background: var(--accent-bg, rgba(74, 144, 217, 0.12));
    border-color: var(--accent-color, #4a90d9);
}

.extension-item input[type="checkbox"] {
    width: 12px;
    height: 12px;
    cursor: pointer;
}

.ext-label {
    font-family: 'JetBrains Mono', 'Consolas', monospace;
    font-size: 0.7rem;
    color: var(--text-color, #e8eaed);
}

/* Expand transition */
.filter-expand-enter-active,
.filter-expand-leave-active {
    transition: all var(--transition-normal, 250ms ease);
    overflow: hidden;
}

.filter-expand-enter-from,
.filter-expand-leave-to {
    opacity: 0;
    max-height: 0;
}

.filter-expand-enter-to,
.filter-expand-leave-from {
    opacity: 1;
    max-height: 500px;
}
</style>
