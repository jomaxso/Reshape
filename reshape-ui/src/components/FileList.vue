<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import type { FileInfo, RenamePreviewItem } from '../types';

const props = defineProps<{
    files: FileInfo[];
    selectedFile?: FileInfo | null;
    previewItems?: RenamePreviewItem[];
}>();

const emit = defineEmits<{
    'select': [file: FileInfo];
    'toggle-selection': [file: FileInfo];
}>();

// Track expanded folders
const expandedFolders = ref<Set<string>>(new Set());

// Build folder tree from preview items or current file paths
interface FolderNode {
    name: string;
    path: string;
    files: FileInfo[];
    subfolders: Map<string, FolderNode>;
    level: number;
}

function buildFolderTree(): FolderNode {
    const root: FolderNode = {
        name: '.',
        path: '',
        files: [],
        subfolders: new Map(),
        level: 0
    };

    // If we have preview items, use those to build the tree
    if (props.previewItems && props.previewItems.length > 0) {
        for (const preview of props.previewItems) {
            const file = props.files.find(f => f.fullPath === preview.fullPath);
            if (!file) continue;

            const newPath = preview.newName;
            const parts = newPath.split(/[\\/]/).filter(p => p.length > 0);

            if (parts.length === 1) {
                // File in root
                root.files.push(file);
            } else {
                // File in subfolder(s)
                let current = root;
                for (let i = 0; i < parts.length - 1; i++) {
                    const folderName = parts[i];
                    if (!folderName) continue; // Skip undefined
                    const folderPath = parts.slice(0, i + 1).join('/');

                    if (!current.subfolders.has(folderName)) {
                        current.subfolders.set(folderName, {
                            name: folderName,
                            path: folderPath,
                            files: [],
                            subfolders: new Map(),
                            level: i + 1
                        });
                    }
                    current = current.subfolders.get(folderName)!;
                }
                current.files.push(file);
            }
        }
    } else {
        // No preview - use original file structure
        for (const file of props.files) {
            const folder = file.relativePath || '.';
            if (folder === '.') {
                root.files.push(file);
            } else {
                const parts = folder.split(/[\\/]/).filter(p => p.length > 0);
                let current = root;
                for (let i = 0; i < parts.length; i++) {
                    const folderName = parts[i];
                    if (!folderName) continue; // Skip undefined
                    const folderPath = parts.slice(0, i + 1).join('/');

                    if (!current.subfolders.has(folderName)) {
                        current.subfolders.set(folderName, {
                            name: folderName,
                            path: folderPath,
                            files: [],
                            subfolders: new Map(),
                            level: i + 1
                        });
                    }
                    current = current.subfolders.get(folderName)!;
                }
                current.files.push(file);
            }
        }
    }

    return root;
}

const folderTree = computed(() => buildFolderTree());

// Flatten tree for easier rendering
interface FlatFolderItem {
    node: FolderNode;
    isExpanded: boolean;
}

function flattenTree(node: FolderNode, result: FlatFolderItem[] = []): FlatFolderItem[] {
    // Add subfolders
    const sortedFolders = Array.from(node.subfolders.values()).sort((a, b) =>
        a.name.localeCompare(b.name, undefined, { numeric: true })
    );

    for (const subfolder of sortedFolders) {
        const isExpanded = expandedFolders.value.has(subfolder.path);
        result.push({ node: subfolder, isExpanded });

        if (isExpanded) {
            flattenTree(subfolder, result);
        }
    }

    return result;
}

const flatFolders = computed(() => {
    const result: FlatFolderItem[] = [];

    // Add root files if any
    if (folderTree.value.files.length > 0) {
        result.push({ node: folderTree.value, isExpanded: true });
    }

    // Add all subfolders
    flattenTree(folderTree.value, result);

    return result;
});

// Keep folders collapsed by default when files change
watch([() => props.files, () => props.previewItems], () => {
    // Ordner bleiben zugeklappt, außer sie waren bereits aufgeklappt
    const currentExpanded = new Set(expandedFolders.value);
    const allFolderPaths = new Set<string>();

    // Collect all folder paths from tree
    function collectPaths(node: FolderNode) {
        for (const subfolder of node.subfolders.values()) {
            allFolderPaths.add(subfolder.path);
            collectPaths(subfolder);
        }
    }
    collectPaths(folderTree.value);

    expandedFolders.value = new Set(
        Array.from(currentExpanded).filter(folder => allFolderPaths.has(folder))
    );
}, { immediate: true });

function toggleFolder(folder: string) {
    if (expandedFolders.value.has(folder)) {
        expandedFolders.value.delete(folder);
    } else {
        expandedFolders.value.add(folder);
    }
    // Trigger reactivity
    expandedFolders.value = new Set(expandedFolders.value);
}

function toggleAllFolders(expand: boolean) {
    if (expand) {
        const allPaths = new Set<string>();
        function collectPaths(node: FolderNode) {
            for (const subfolder of node.subfolders.values()) {
                allPaths.add(subfolder.path);
                collectPaths(subfolder);
            }
        }
        collectPaths(folderTree.value);
        expandedFolders.value = allPaths;
    } else {
        expandedFolders.value = new Set();
    }
}

function toggleSelection(event: Event, file: FileInfo) {
    event.stopPropagation();
    emit('toggle-selection', file);
}

function toggleFolderSelection(_folder: string, files: FileInfo[]) {
    // Wenn alle Dateien ausgewählt sind → alle abwählen
    // Sonst → alle auswählen
    const allSelected = files.every(f => f.isSelected);
    const shouldSelect = !allSelected;

    for (const file of files) {
        // Nur umschalten, wenn der gewünschte Zustand nicht bereits erreicht ist
        if (file.isSelected !== shouldSelect) {
            emit('toggle-selection', file);
        }
    }
}

function formatSize(bytes: number): string {
    const sizes = ['B', 'KB', 'MB', 'GB'];
    let order = 0;
    let size = bytes;
    while (size >= 1024 && order < sizes.length - 1) {
        order++;
        size /= 1024;
    }
    return `${size.toFixed(1)} ${sizes[order]}`;
}

function formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('de-DE', {
        day: '2-digit',
        month: '2-digit',
        year: '2-digit',
        hour: '2-digit',
        minute: '2-digit',
    });
}

function getFileIcon(extension: string): string {
    const ext = extension.toLowerCase();
    if (['.jpg', '.jpeg', '.png', '.gif', '.bmp', '.webp', '.heic'].includes(ext)) return '🖼️';
    if (['.mp4', '.mov', '.avi', '.mkv', '.wmv'].includes(ext)) return '🎬';
    if (['.mp3', '.wav', '.flac', '.aac', '.ogg'].includes(ext)) return '🎵';
    if (['.pdf'].includes(ext)) return '📄';
    if (['.doc', '.docx', '.txt', '.md'].includes(ext)) return '📝';
    if (['.zip', '.rar', '.7z', '.tar', '.gz'].includes(ext)) return '📦';
    return '📎';
}

const stats = computed(() => {
    const total = props.files.length;
    const selected = props.files.filter(f => f.isSelected).length;
    const totalSize = props.files.reduce((sum, f) => sum + f.size, 0);
    return { total, selected, totalSize };
});
</script>

<template>
    <div class="file-list">
        <!-- Toolbar -->
        <div class="toolbar">
            <div class="toolbar-left">
                <span class="file-count">
                    <span class="count-selected">{{ stats.selected }}</span>
                    <span class="count-divider">/</span>
                    <span class="count-total">{{ stats.total }}</span>
                    <span class="count-label">Dateien</span>
                </span>
                <span class="total-size">{{ formatSize(stats.totalSize) }}</span>
            </div>
            <div class="toolbar-right">
                <button class="toolbar-btn" @click="toggleAllFolders(true)" title="Alle Ordner aufklappen">
                    <span>⬇️</span>
                </button>
                <button class="toolbar-btn" @click="toggleAllFolders(false)" title="Alle Ordner einklappen">
                    <span>⬆️</span>
                </button>
            </div>
        </div>

        <!-- File Tree -->
        <div class="files-container">
            <template v-if="flatFolders.length > 0">
                <div v-for="item in flatFolders" :key="item.node.path || 'root'" class="folder-group">
                    <!-- Folder Header -->
                    <div class="folder-header" :style="{ paddingLeft: `${item.node.level * 1.25}rem` }"
                        @click="toggleFolder(item.node.path)">
                        <span class="folder-toggle">
                            {{ item.isExpanded ? '▼' : '▶' }}
                        </span>
                        <span class="folder-icon">📁</span>
                        <span class="folder-name">{{ item.node.name === '.' ? 'Stammverzeichnis' : item.node.name
                            }}</span>
                        <span class="folder-count">({{ item.node.files.length }})</span>
                        <label class="folder-checkbox" @click.stop>
                            <input type="checkbox" :checked="item.node.files.every(f => f.isSelected)"
                                :indeterminate="item.node.files.some(f => f.isSelected) && !item.node.files.every(f => f.isSelected)"
                                @change="toggleFolderSelection(item.node.path, item.node.files)" />
                        </label>
                    </div>

                    <!-- Files in Folder -->
                    <Transition name="folder-slide">
                        <div v-if="item.isExpanded" class="folder-files">
                            <div v-for="file in item.node.files" :key="file.fullPath" :class="[
                                'file-row',
                                {
                                    selected: selectedFile?.fullPath === file.fullPath,
                                    unselected: !file.isSelected
                                }
                            ]" :style="{ paddingLeft: `${(item.node.level + 1) * 1.25}rem` }"
                                @click="emit('select', file)">
                                <label class="file-checkbox" @click.stop>
                                    <input type="checkbox" :checked="file.isSelected"
                                        @click="toggleSelection($event, file)" />
                                </label>
                                <span class="file-icon">{{ getFileIcon(file.extension) }}</span>
                                <span class="file-name" :title="file.fullPath">{{ file.name }}</span>
                                <span class="file-size">{{ formatSize(file.size) }}</span>
                                <span class="file-date">{{ formatDate(file.modifiedAt) }}</span>
                            </div>
                        </div>
                    </Transition>
                </div>
            </template>

            <div v-else class="empty-state">
                <span class="empty-icon">📂</span>
                <p>Keine Dateien gefunden</p>
            </div>
        </div>
    </div>
</template>

<style scoped>
.file-list {
    background: var(--bg-card, #22242e);
    border: 1px solid var(--border-color, #2e313d);
    border-radius: var(--radius-md, 10px);
    overflow: hidden;
    display: flex;
    flex-direction: column;
}

.toolbar {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 0.75rem 1rem;
    background: var(--bg-tertiary, #1f2028);
    border-bottom: 1px solid var(--border-color, #2e313d);
}

.toolbar-left {
    display: flex;
    align-items: center;
    gap: 1rem;
}

.file-count {
    display: flex;
    align-items: center;
    gap: 0.25rem;
    font-size: 0.85rem;
}

.count-selected {
    font-weight: 600;
    color: var(--accent-color, #4a90d9);
}

.count-divider {
    color: var(--text-dim, #4b5563);
}

.count-total {
    color: var(--text-secondary, #b8bcc4);
}

.count-label {
    color: var(--text-muted, #6b7280);
    margin-left: 0.25rem;
}

.total-size {
    font-size: 0.8rem;
    color: var(--text-dim, #4b5563);
    padding: 0.2rem 0.5rem;
    background: var(--bg-card, #22242e);
    border-radius: 4px;
}

.toolbar-right {
    display: flex;
    gap: 0.5rem;
}

.toolbar-btn {
    padding: 0.375rem 0.5rem;
    background: var(--bg-card, #22242e);
    border: 1px solid var(--border-color, #2e313d);
    border-radius: var(--radius-sm, 6px);
    cursor: pointer;
    font-size: 0.75rem;
    transition: all var(--transition-fast, 150ms ease);
}

.toolbar-btn:hover {
    background: var(--bg-hover, #2a2d3a);
    border-color: var(--text-muted, #6b7280);
}

.files-container {
    flex: 1;
    min-height: 400px;
    max-height: 600px;
    overflow-y: auto;
}

.folder-group {
    border-bottom: 1px solid var(--border-light, #252830);
}

.folder-group:last-child {
    border-bottom: none;
}

.folder-header {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.625rem 1rem;
    background: var(--bg-secondary, #17181c);
    cursor: pointer;
    user-select: none;
    transition: background var(--transition-fast, 150ms ease);
}

.folder-header:hover {
    background: var(--bg-hover, #2a2d3a);
}

.folder-toggle {
    font-size: 0.65rem;
    color: var(--text-muted, #6b7280);
    width: 1rem;
    text-align: center;
}

.folder-icon {
    font-size: 1rem;
}

.folder-name {
    flex: 1;
    font-size: 0.875rem;
    font-weight: 500;
    color: var(--text-color, #e8eaed);
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.folder-count {
    font-size: 0.75rem;
    color: var(--text-dim, #4b5563);
}

.folder-checkbox {
    margin-left: auto;
}

.folder-checkbox input {
    width: 14px;
    height: 14px;
    cursor: pointer;
}

.folder-files {
    background: var(--bg-card, #22242e);
}

.file-row {
    display: grid;
    grid-template-columns: 32px 28px 1fr auto auto;
    gap: 0.5rem;
    align-items: center;
    padding: 0.5rem 1rem 0.5rem 2rem;
    cursor: pointer;
    transition: background var(--transition-fast, 150ms ease);
    border-bottom: 1px solid var(--border-light, #252830);
}

.file-row:last-child {
    border-bottom: none;
}

.file-row:hover {
    background: var(--bg-hover, #2a2d3a);
}

.file-row.selected {
    background: var(--accent-bg, rgba(74, 144, 217, 0.12));
    border-left: 3px solid var(--accent-color, #4a90d9);
    padding-left: calc(2rem - 3px);
}

.file-row.unselected {
    opacity: 0.45;
}

.file-checkbox input {
    width: 14px;
    height: 14px;
    cursor: pointer;
}

.file-icon {
    font-size: 1rem;
    text-align: center;
}

.file-name {
    font-family: 'JetBrains Mono', 'Consolas', monospace;
    font-size: 0.8rem;
    color: var(--text-color, #e8eaed);
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.file-size {
    font-size: 0.75rem;
    color: var(--text-dim, #4b5563);
    text-align: right;
    min-width: 60px;
}

.file-date {
    font-size: 0.7rem;
    color: var(--text-dim, #4b5563);
    min-width: 100px;
    text-align: right;
}

.empty-state {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 4rem 2rem;
    color: var(--text-muted, #6b7280);
}

.empty-icon {
    font-size: 3rem;
    margin-bottom: 1rem;
    opacity: 0.5;
}

/* Folder slide transition */
.folder-slide-enter-active,
.folder-slide-leave-active {
    transition: all var(--transition-normal, 250ms ease);
    overflow: hidden;
}

.folder-slide-enter-from,
.folder-slide-leave-to {
    opacity: 0;
    max-height: 0;
}

.folder-slide-enter-to,
.folder-slide-leave-from {
    opacity: 1;
    max-height: 2000px;
}

/* Scrollbar styling */
.files-container::-webkit-scrollbar {
    width: 8px;
}

.files-container::-webkit-scrollbar-track {
    background: var(--bg-card, #22242e);
}

.files-container::-webkit-scrollbar-thumb {
    background: var(--border-color, #2e313d);
    border-radius: 4px;
}

.files-container::-webkit-scrollbar-thumb:hover {
    background: var(--text-muted, #6b7280);
}
</style>
