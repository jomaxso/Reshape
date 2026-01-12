<script setup lang="ts">
import { ref, onMounted } from 'vue';
import FolderPicker from './components/FolderPicker.vue';
import FileList from './components/FileList.vue';
import MetadataPanel from './components/MetadataPanel.vue';
import RenamePreview from './components/RenamePreview.vue';
import api from './api';
import type { FileInfo, RenamePattern, RenamePreviewItem } from './types';

// State
const folderPath = ref('');
const files = ref<FileInfo[]>([]);
const selectedFile = ref<FileInfo | null>(null);
const patterns = ref<RenamePattern[]>([]);
const previewItems = ref<RenamePreviewItem[]>([]);
const currentPattern = ref('');

const loading = ref(false);
const previewLoading = ref(false);
const error = ref<string | null>(null);
const successMessage = ref<string | null>(null);

// Load patterns on mount
onMounted(async () => {
  try {
    patterns.value = await api.getPatterns();
  } catch (e) {
    console.error('Failed to load patterns:', e);
  }
});

// Scan folder
async function handleScan() {
  if (!folderPath.value) return;

  loading.value = true;
  error.value = null;
  successMessage.value = null;
  previewItems.value = [];
  selectedFile.value = null;

  try {
    const response = await api.scanFolder(folderPath.value);
    files.value = response.files;

    if (response.files.length === 0) {
      error.value = 'No files found in this folder';
    }
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to scan folder';
    files.value = [];
  } finally {
    loading.value = false;
  }
}

// Generate preview
async function handlePreview(pattern: string) {
  if (!folderPath.value || !pattern) return;

  currentPattern.value = pattern;
  previewLoading.value = true;
  error.value = null;

  try {
    const response = await api.previewRename(folderPath.value, pattern);
    previewItems.value = response.items;
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to generate preview';
  } finally {
    previewLoading.value = false;
  }
}

// Execute rename
async function handleExecute() {
  if (previewItems.value.length === 0) return;

  loading.value = true;
  error.value = null;
  successMessage.value = null;

  try {
    const response = await api.executeRename(previewItems.value);

    if (response.errorCount > 0) {
      error.value = `${response.errorCount} file(s) failed to rename`;
    }

    if (response.successCount > 0) {
      successMessage.value = `✅ Successfully renamed ${response.successCount} file(s)`;

      // Refresh file list and preview
      await handleScan();
      if (currentPattern.value) {
        await handlePreview(currentPattern.value);
      }
    }
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to execute rename';
  } finally {
    loading.value = false;
  }
}

// Select file
function handleSelectFile(file: FileInfo) {
  selectedFile.value = selectedFile.value?.fullPath === file.fullPath ? null : file;
}
</script>

<template>
  <div class="app">
    <header class="header">
      <h1>🔄 Reshape</h1>
      <p class="subtitle">Batch rename files using metadata patterns</p>
    </header>

    <main class="main">
      <!-- Folder Picker -->
      <FolderPicker v-model="folderPath" :loading="loading" @scan="handleScan" />

      <!-- Error / Success Messages -->
      <div v-if="error" class="message error">
        ⚠️ {{ error }}
      </div>
      <div v-if="successMessage" class="message success">
        {{ successMessage }}
      </div>

      <!-- Content Grid -->
      <div v-if="files.length > 0" class="content-grid">
        <!-- Left: File List -->
        <div class="panel files-panel">
          <h2>📂 Files ({{ files.length }})</h2>
          <FileList :files="files" :selected-file="selectedFile" @select="handleSelectFile" />
        </div>

        <!-- Right: Metadata + Rename -->
        <div class="panel rename-panel">
          <!-- Metadata Panel (when file selected) -->
          <MetadataPanel v-if="selectedFile" :file="selectedFile" class="metadata-section" />

          <!-- Rename Preview -->
          <RenamePreview :patterns="patterns" :preview-items="previewItems" :loading="previewLoading"
            @preview="handlePreview" @execute="handleExecute" />
        </div>
      </div>

      <!-- Empty State -->
      <div v-else-if="!loading && !error" class="empty-state">
        <span class="empty-icon">📁</span>
        <h2>Enter a folder path to get started</h2>
        <p>Scan a folder to view files and rename them based on metadata patterns</p>
      </div>
    </main>

    <footer class="footer">
      <p>Reshape CLI • Run <code>reshape --help</code> for command-line options</p>
    </footer>
  </div>
</template>

<style>
:root {
  --bg-primary: #121212;
  --bg-secondary: #1e1e1e;
  --bg-tertiary: #252526;
  --bg-hover: #2a2a2a;
  --border-color: #3e3e3e;
  --border-light: #2d2d2d;
  --text-color: #ffffff;
  --text-muted: #888888;
  --accent-color: #007acc;
  --accent-hover: #0098ff;
  --accent-bg: rgba(0, 122, 204, 0.2);
  --error-color: #f44336;
  --success-color: #4caf50;
}

* {
  box-sizing: border-box;
  margin: 0;
  padding: 0;
}

body {
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, sans-serif;
  background: var(--bg-primary);
  color: var(--text-color);
  line-height: 1.6;
}
</style>

<style scoped>
.app {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}

.header {
  padding: 1.5rem 2rem;
  background: var(--bg-secondary);
  border-bottom: 1px solid var(--border-color);
  text-align: center;
}

.header h1 {
  font-size: 1.75rem;
  margin-bottom: 0.25rem;
}

.subtitle {
  color: var(--text-muted);
  font-size: 0.95rem;
}

.main {
  flex: 1;
  padding: 2rem;
  max-width: 1400px;
  margin: 0 auto;
  width: 100%;
}

.message {
  padding: 1rem 1.25rem;
  border-radius: 8px;
  margin-bottom: 1.5rem;
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.message.error {
  background: rgba(244, 67, 54, 0.15);
  border: 1px solid rgba(244, 67, 54, 0.3);
  color: #ff6b6b;
}

.message.success {
  background: rgba(76, 175, 80, 0.15);
  border: 1px solid rgba(76, 175, 80, 0.3);
  color: #69f0ae;
}

.content-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1.5rem;
}

@media (max-width: 1024px) {
  .content-grid {
    grid-template-columns: 1fr;
  }
}

.panel {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.panel h2 {
  font-size: 1.1rem;
  color: var(--text-color);
}

.metadata-section {
  margin-bottom: 1rem;
}

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 4rem 2rem;
  text-align: center;
}

.empty-icon {
  font-size: 4rem;
  margin-bottom: 1.5rem;
  opacity: 0.5;
}

.empty-state h2 {
  margin-bottom: 0.5rem;
  color: var(--text-color);
}

.empty-state p {
  color: var(--text-muted);
}

.footer {
  padding: 1rem 2rem;
  text-align: center;
  color: var(--text-muted);
  font-size: 0.85rem;
  border-top: 1px solid var(--border-color);
}

.footer code {
  background: var(--bg-tertiary);
  padding: 0.2rem 0.5rem;
  border-radius: 4px;
  font-family: 'Consolas', 'Monaco', monospace;
}
</style>
