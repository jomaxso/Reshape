<script setup lang="ts">
import { ref, computed } from 'vue';

const props = defineProps<{
    modelValue: string;
    loading?: boolean;
}>();

const emit = defineEmits<{
    'update:modelValue': [value: string];
    'scan': [];
}>();

const inputPath = ref(props.modelValue);

const isValidPath = computed(() => {
    // Basic path validation - looks like a Windows or Unix path
    const path = inputPath.value.trim();
    return path.length > 0 && (
        /^[a-zA-Z]:\\/.test(path) || // Windows: C:\...
        /^\//.test(path) ||          // Unix: /...
        /^\.\.?\//.test(path)        // Relative: ./ or ../
    );
});

function handleScan() {
    if (!isValidPath.value || props.loading) return;
    emit('update:modelValue', inputPath.value.trim());
    emit('scan');
}

function handleKeydown(e: KeyboardEvent) {
    if (e.key === 'Enter') {
        handleScan();
    }
}
</script>

<template>
    <div class="folder-picker">
        <div class="input-group">
            <span class="icon">📁</span>
            <input v-model="inputPath" type="text"
                placeholder="Enter folder path (e.g., C:\Photos or /home/user/images)" :disabled="loading"
                @keydown="handleKeydown" class="path-input" />
            <button @click="handleScan" :disabled="!isValidPath || loading" class="scan-button">
                <span v-if="loading" class="spinner"></span>
                <span v-else>🔍 Scan</span>
            </button>
        </div>
        <p v-if="inputPath && !isValidPath" class="hint error">
            Please enter a valid folder path
        </p>
    </div>
</template>

<style scoped>
.folder-picker {
    margin-bottom: 1.5rem;
}

.input-group {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    background: var(--bg-secondary, #1e1e1e);
    border: 2px solid var(--border-color, #3e3e3e);
    border-radius: 8px;
    padding: 0.5rem;
    transition: border-color 0.2s;
}

.input-group:focus-within {
    border-color: var(--accent-color, #007acc);
}

.icon {
    font-size: 1.25rem;
    padding: 0 0.5rem;
}

.path-input {
    flex: 1;
    background: transparent;
    border: none;
    color: var(--text-color, #fff);
    font-size: 1rem;
    padding: 0.5rem;
    outline: none;
    font-family: 'Consolas', 'Monaco', monospace;
}

.path-input::placeholder {
    color: var(--text-muted, #888);
}

.path-input:disabled {
    opacity: 0.6;
}

.scan-button {
    background: var(--accent-color, #007acc);
    color: white;
    border: none;
    border-radius: 6px;
    padding: 0.625rem 1.25rem;
    font-size: 0.9rem;
    font-weight: 600;
    cursor: pointer;
    transition: background-color 0.2s, transform 0.1s;
    display: flex;
    align-items: center;
    gap: 0.5rem;
}

.scan-button:hover:not(:disabled) {
    background: var(--accent-hover, #0098ff);
    transform: translateY(-1px);
}

.scan-button:disabled {
    opacity: 0.5;
    cursor: not-allowed;
}

.spinner {
    width: 16px;
    height: 16px;
    border: 2px solid rgba(255, 255, 255, 0.3);
    border-top-color: white;
    border-radius: 50%;
    animation: spin 0.8s linear infinite;
}

@keyframes spin {
    to {
        transform: rotate(360deg);
    }
}

.hint {
    margin: 0.5rem 0 0 0.5rem;
    font-size: 0.85rem;
    color: var(--text-muted, #888);
}

.hint.error {
    color: var(--error-color, #f44336);
}
</style>
