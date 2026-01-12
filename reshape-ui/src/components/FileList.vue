<script setup lang="ts">
import { computed } from 'vue';
import type { FileInfo } from '../types';

const props = defineProps<{
    files: FileInfo[];
    selectedFile?: FileInfo | null;
}>();

const emit = defineEmits<{
    'select': [file: FileInfo];
}>();

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
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
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

const sortedFiles = computed(() => {
    return [...props.files].sort((a, b) => a.name.localeCompare(b.name));
});
</script>

<template>
    <div class="file-list">
        <div class="header">
            <span class="col-icon"></span>
            <span class="col-name">Name</span>
            <span class="col-size">Size</span>
            <span class="col-date">Modified</span>
        </div>

        <div class="files-container">
            <div v-for="file in sortedFiles" :key="file.fullPath"
                :class="['file-row', { selected: selectedFile?.fullPath === file.fullPath }]"
                @click="emit('select', file)">
                <span class="col-icon">{{ getFileIcon(file.extension) }}</span>
                <span class="col-name" :title="file.fullPath">{{ file.name }}</span>
                <span class="col-size">{{ formatSize(file.size) }}</span>
                <span class="col-date">{{ formatDate(file.modifiedAt) }}</span>
            </div>

            <div v-if="files.length === 0" class="empty-state">
                <span class="empty-icon">📂</span>
                <p>No files found in this folder</p>
            </div>
        </div>
    </div>
</template>

<style scoped>
.file-list {
    background: var(--bg-secondary, #1e1e1e);
    border: 1px solid var(--border-color, #3e3e3e);
    border-radius: 8px;
    overflow: hidden;
}

.header {
    display: grid;
    grid-template-columns: 40px 1fr 100px 150px;
    gap: 0.5rem;
    padding: 0.75rem 1rem;
    background: var(--bg-tertiary, #252526);
    border-bottom: 1px solid var(--border-color, #3e3e3e);
    font-weight: 600;
    font-size: 0.85rem;
    color: var(--text-muted, #888);
    text-transform: uppercase;
    letter-spacing: 0.5px;
}

.files-container {
    max-height: 400px;
    overflow-y: auto;
}

.file-row {
    display: grid;
    grid-template-columns: 40px 1fr 100px 150px;
    gap: 0.5rem;
    padding: 0.625rem 1rem;
    cursor: pointer;
    transition: background-color 0.15s;
    border-bottom: 1px solid var(--border-light, #2d2d2d);
}

.file-row:hover {
    background: var(--bg-hover, #2a2a2a);
}

.file-row.selected {
    background: var(--accent-bg, rgba(0, 122, 204, 0.2));
    border-left: 3px solid var(--accent-color, #007acc);
}

.col-icon {
    text-align: center;
    font-size: 1.1rem;
}

.col-name {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    font-family: 'Consolas', 'Monaco', monospace;
}

.col-size {
    text-align: right;
    color: var(--text-muted, #888);
    font-size: 0.9rem;
}

.col-date {
    color: var(--text-muted, #888);
    font-size: 0.85rem;
}

.empty-state {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 3rem;
    color: var(--text-muted, #888);
}

.empty-icon {
    font-size: 3rem;
    margin-bottom: 1rem;
    opacity: 0.5;
}

/* Scrollbar styling */
.files-container::-webkit-scrollbar {
    width: 8px;
}

.files-container::-webkit-scrollbar-track {
    background: var(--bg-secondary, #1e1e1e);
}

.files-container::-webkit-scrollbar-thumb {
    background: var(--border-color, #3e3e3e);
    border-radius: 4px;
}

.files-container::-webkit-scrollbar-thumb:hover {
    background: var(--text-muted, #888);
}
</style>
