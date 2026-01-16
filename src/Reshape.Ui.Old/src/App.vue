<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import FolderPicker from './components/FolderPicker.vue';
import FileList from './components/FileList.vue';
import MetadataPanel from './components/MetadataPanel.vue';
import VacationModePanel from './components/VacationModePanel.vue';
import GeneralModePanel from './components/GeneralModePanel.vue';
import PlaceholderReference from './components/PlaceholderReference.vue';
import PatternManager from './components/PatternManager.vue';
import api from './api';
import type { FileInfo, RenamePattern, RenamePreviewItem, VacationModeOptions } from './types';

// Constants
const DEFAULT_PATTERN_COUNT = 6; // Number of built-in patterns

// State
const folderPath = ref('');
const files = ref<FileInfo[]>([]);
const selectedFile = ref<FileInfo | null>(null);
const patterns = ref<RenamePattern[]>([]);
const generalPreviewItems = ref<RenamePreviewItem[]>([]);
const vacationPreviewItems = ref<RenamePreviewItem[]>([]);

// Mode state - only one mode active at a time (accordion pattern)
const activeMode = ref<'general' | 'vacation' | null>('general');

const loading = ref(false);
const generalLoading = ref(false);
const vacationLoading = ref(false);
const error = ref<string | null>(null);
const successMessage = ref<string | null>(null);

// Toggle mode (accordion behavior - only one open at a time)
function handleModeChange(mode: 'general' | 'vacation', isActive: boolean) {
  if (isActive) {
    activeMode.value = mode;
    // Clear the other mode's preview when switching
    if (mode === 'general') {
      vacationPreviewItems.value = [];
    } else {
      generalPreviewItems.value = [];
    }
  } else {
    activeMode.value = null;
  }
}

// Get current preview items based on active mode
const currentPreviewItems = computed(() => {
  if (activeMode.value === 'general') {
    return generalPreviewItems.value;
  } else if (activeMode.value === 'vacation') {
    return vacationPreviewItems.value;
  }
  return [];
});

// Load patterns on mount
onMounted(async () => {
  try {
    patterns.value = await api.getPatterns();
  } catch (e) {
    console.error('Failed to load patterns:', e);
  }
});

// Refresh patterns
async function refreshPatterns() {
  try {
    patterns.value = await api.getPatterns();
  } catch (e) {
    console.error('Failed to refresh patterns:', e);
  }
}

// Add custom pattern
async function handleAddPattern(pattern: string, description: string) {
  try {
    const response = await api.addPattern(pattern, description);
    if (!response.success) {
      throw new Error(response.message || 'Failed to add pattern');
    }
    // Refresh patterns immediately after successful add
    await refreshPatterns();
  } catch (e) {
    throw e;
  }
}

// Remove custom pattern
async function handleRemovePattern(pattern: string) {
  try {
    const response = await api.removePattern(pattern);
    if (!response.success) {
      throw new Error(response.message || 'Failed to remove pattern');
    }
    // Refresh patterns immediately after successful remove
    await refreshPatterns();
  } catch (e) {
    throw e;
  }
}

// Scan folder
async function handleScan() {
  if (!folderPath.value) return;

  loading.value = true;
  error.value = null;
  successMessage.value = null;
  generalPreviewItems.value = [];
  vacationPreviewItems.value = [];

  try {
    const response = await api.scanFolder(folderPath.value);
    files.value = response.files;

    // Auto-select first file
    const firstFile = response.files[0];
    if (firstFile) {
      selectedFile.value = firstFile;
    } else {
      selectedFile.value = null;
      error.value = 'No files found in this folder';
    }
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to scan folder';
    files.value = [];
    selectedFile.value = null;
  } finally {
    loading.value = false;
  }
}

// Generate general preview
async function handleGeneralPreview(pattern: string) {
  if (!folderPath.value || !pattern) return;

  generalLoading.value = true;
  error.value = null;

  try {
    const response = await api.previewRename(folderPath.value, pattern);
    generalPreviewItems.value = response.items;
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to generate preview';
  } finally {
    generalLoading.value = false;
  }
}

// Generate vacation preview
async function handleVacationPreview({ pattern, vacationMode }: { pattern: string; vacationMode: VacationModeOptions }) {
  if (!folderPath.value || !pattern || !vacationMode.enabled) {
    vacationPreviewItems.value = [];
    return;
  }

  vacationLoading.value = true;
  error.value = null;

  try {
    const response = await api.previewRename(folderPath.value, pattern, undefined, vacationMode);
    vacationPreviewItems.value = response.items;
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to generate preview';
  } finally {
    vacationLoading.value = false;
  }
}

// Execute rename (general mode)
async function handleGeneralExecute() {
  if (generalPreviewItems.value.length === 0) return;

  loading.value = true;
  error.value = null;
  successMessage.value = null;

  try {
    const response = await api.executeRename(generalPreviewItems.value, folderPath.value);

    if (response.errorCount > 0) {
      error.value = `${response.errorCount} Datei(en) konnten nicht umbenannt werden`;
    }

    if (response.successCount > 0) {
      successMessage.value = `✅ ${response.successCount} Datei(en) erfolgreich umbenannt`;
      await handleScan();
      generalPreviewItems.value = [];
    }
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to execute rename';
  } finally {
    loading.value = false;
  }
}

// Execute rename (vacation mode)
async function handleVacationExecute() {
  if (vacationPreviewItems.value.length === 0) return;

  loading.value = true;
  error.value = null;
  successMessage.value = null;

  try {
    const response = await api.executeRename(vacationPreviewItems.value, folderPath.value);

    if (response.errorCount > 0) {
      error.value = `${response.errorCount} Datei(en) konnten nicht umbenannt werden`;
    }

    if (response.successCount > 0) {
      successMessage.value = `✅ ${response.successCount} Datei(en) erfolgreich umbenannt`;
      await handleScan();
      vacationPreviewItems.value = [];
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

// Toggle file selection
function handleToggleFileSelection(file: FileInfo) {
  files.value = files.value.map(f =>
    f.fullPath === file.fullPath
      ? { ...f, isSelected: !f.isSelected }
      : f
  );

  // Update in both preview lists
  generalPreviewItems.value = generalPreviewItems.value.map(p =>
    p.fullPath === file.fullPath
      ? { ...p, isSelected: !file.isSelected }
      : p
  );

  vacationPreviewItems.value = vacationPreviewItems.value.map(p =>
    p.fullPath === file.fullPath
      ? { ...p, isSelected: !file.isSelected }
      : p
  );
}

// Toggle general preview item selection
function handleToggleGeneralItem(item: RenamePreviewItem) {
  generalPreviewItems.value = generalPreviewItems.value.map(p =>
    p.fullPath === item.fullPath
      ? { ...p, isSelected: !p.isSelected }
      : p
  );

  files.value = files.value.map(f =>
    f.fullPath === item.fullPath
      ? { ...f, isSelected: !item.isSelected }
      : f
  );
}

// Toggle vacation preview item selection
function handleToggleVacationItem(item: RenamePreviewItem) {
  vacationPreviewItems.value = vacationPreviewItems.value.map(p =>
    p.fullPath === item.fullPath
      ? { ...p, isSelected: !p.isSelected }
      : p
  );

  files.value = files.value.map(f =>
    f.fullPath === item.fullPath
      ? { ...f, isSelected: !item.isSelected }
      : f
  );
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
        <!-- Left: File List & Details -->
        <div class="panel files-panel">
          <div class="panel-title">
            <span class="panel-icon">📂</span>
            <span>Dateien ({{ files.length }})</span>
          </div>
          <FileList :files="files" :selected-file="selectedFile" :preview-items="currentPreviewItems"
            @select="handleSelectFile" @toggle-selection="handleToggleFileSelection" />

          <!-- File Details below file list -->
          <MetadataPanel v-if="selectedFile" :file="selectedFile" class="metadata-section" />
        </div>

        <!-- Right: Rename Modes -->
        <div class="panel rename-panel">
          <!-- Mode Selection Info -->
          <div v-if="activeMode === null" class="mode-hint">
            <span class="hint-icon">💡</span>
            <span>Wähle einen Modus um zu beginnen</span>
          </div>

          <!-- General Mode Panel -->
          <GeneralModePanel :patterns="patterns" :preview-items="generalPreviewItems" :loading="generalLoading"
            :is-active="activeMode === 'general'"
            @mode-change="(active: boolean) => handleModeChange('general', active)" @preview="handleGeneralPreview"
            @execute="handleGeneralExecute" @toggle-item="handleToggleGeneralItem" />

          <!-- Vacation Mode Panel -->
          <VacationModePanel file-pattern="" :preview-items="vacationPreviewItems" :loading="vacationLoading"
            :files="files" :is-active="activeMode === 'vacation'"
            @mode-change="(active: boolean) => handleModeChange('vacation', active)" @preview="handleVacationPreview"
            @execute="handleVacationExecute" @toggle-item="handleToggleVacationItem" />

          <!-- Placeholder Reference (for all modes) -->
          <PlaceholderReference />

          <!-- Pattern Manager -->
          <PatternManager :patterns="patterns" :default-pattern-count="DEFAULT_PATTERN_COUNT" @add="handleAddPattern"
            @remove="handleRemovePattern" />

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
  /* Modern color palette with smooth gradients */
  --bg-primary: #0f0f11;
  --bg-secondary: #17181c;
  --bg-tertiary: #1f2028;
  --bg-card: #22242e;
  --bg-hover: #2a2d3a;
  --bg-active: #323644;

  --border-color: #2e313d;
  --border-light: #252830;
  --border-focus: #4a90d9;

  --text-color: #e8eaed;
  --text-secondary: #b8bcc4;
  --text-muted: #6b7280;
  --text-dim: #4b5563;

  --accent-color: #4a90d9;
  --accent-hover: #5ba0e9;
  --accent-bg: rgba(74, 144, 217, 0.12);
  --accent-border: rgba(74, 144, 217, 0.3);

  --success-color: #34d399;
  --success-bg: rgba(52, 211, 153, 0.12);
  --success-border: rgba(52, 211, 153, 0.3);

  --warning-color: #fbbf24;
  --warning-bg: rgba(251, 191, 36, 0.12);
  --warning-border: rgba(251, 191, 36, 0.3);

  --error-color: #f87171;
  --error-bg: rgba(248, 113, 113, 0.12);
  --error-border: rgba(248, 113, 113, 0.3);

  --vacation-color: #f59e0b;
  --vacation-bg: rgba(245, 158, 11, 0.12);
  --vacation-border: rgba(245, 158, 11, 0.3);

  --general-color: #8b5cf6;
  --general-bg: rgba(139, 92, 246, 0.12);
  --general-border: rgba(139, 92, 246, 0.3);

  /* Shadows */
  --shadow-sm: 0 1px 2px rgba(0, 0, 0, 0.3);
  --shadow-md: 0 4px 6px rgba(0, 0, 0, 0.4);
  --shadow-lg: 0 10px 15px rgba(0, 0, 0, 0.5);

  /* Transitions */
  --transition-fast: 150ms ease;
  --transition-normal: 250ms ease;

  /* Border radius */
  --radius-sm: 6px;
  --radius-md: 10px;
  --radius-lg: 14px;
}

* {
  box-sizing: border-box;
  margin: 0;
  padding: 0;
}

body {
  font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, sans-serif;
  background: var(--bg-primary);
  color: var(--text-color);
  line-height: 1.6;
  font-size: 14px;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
}
</style>

<style scoped>
.app {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  background: linear-gradient(180deg, var(--bg-primary) 0%, var(--bg-secondary) 100%);
}

.header {
  padding: 1.25rem 2rem;
  background: var(--bg-secondary);
  border-bottom: 1px solid var(--border-color);
  display: flex;
  align-items: center;
  gap: 1rem;
}

.header h1 {
  font-size: 1.5rem;
  font-weight: 700;
  letter-spacing: -0.02em;
  background: linear-gradient(135deg, var(--accent-color) 0%, #8b5cf6 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.subtitle {
  color: var(--text-muted);
  font-size: 0.875rem;
  font-weight: 400;
}

.main {
  flex: 1;
  padding: 1.5rem;
  max-width: 1600px;
  margin: 0 auto;
  width: 100%;
}

.message {
  padding: 0.875rem 1rem;
  border-radius: var(--radius-md);
  margin-bottom: 1.25rem;
  display: flex;
  align-items: center;
  gap: 0.625rem;
  font-size: 0.9rem;
  font-weight: 500;
  backdrop-filter: blur(8px);
}

.message.error {
  background: var(--error-bg);
  border: 1px solid var(--error-border);
  color: var(--error-color);
}

.message.success {
  background: var(--success-bg);
  border: 1px solid var(--success-border);
  color: var(--success-color);
}

.content-grid {
  display: grid;
  grid-template-columns: 1.2fr 1fr;
  gap: 1.5rem;
  align-items: start;
}

@media (max-width: 1200px) {
  .content-grid {
    grid-template-columns: 1fr;
  }
}

.panel {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.panel-title {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 1rem;
  font-weight: 600;
  color: var(--text-color);
  padding: 0.5rem 0;
}

.panel-icon {
  font-size: 1.1rem;
}

.files-panel {
  min-height: 500px;
}

.rename-panel {
  position: sticky;
  top: 1.5rem;
}

.mode-hint {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 1.5rem;
  background: var(--bg-card);
  border: 1px dashed var(--border-color);
  border-radius: var(--radius-md);
  color: var(--text-muted);
  font-size: 0.9rem;
}

.hint-icon {
  font-size: 1.25rem;
}

.metadata-section {
  margin-bottom: 0.5rem;
}

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 5rem 2rem;
  text-align: center;
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  border-radius: var(--radius-lg);
  margin-top: 2rem;
}

.empty-icon {
  font-size: 4rem;
  margin-bottom: 1.5rem;
  opacity: 0.6;
}

.empty-state h2 {
  margin-bottom: 0.5rem;
  color: var(--text-color);
  font-weight: 600;
}

.empty-state p {
  color: var(--text-muted);
  max-width: 400px;
}

.footer {
  padding: 1rem 2rem;
  text-align: center;
  color: var(--text-dim);
  font-size: 0.8rem;
  border-top: 1px solid var(--border-color);
  background: var(--bg-secondary);
}

.footer code {
  background: var(--bg-tertiary);
  padding: 0.2rem 0.5rem;
  border-radius: var(--radius-sm);
  font-family: 'JetBrains Mono', 'Consolas', 'Monaco', monospace;
  font-size: 0.75rem;
  color: var(--text-secondary);
}
</style>
