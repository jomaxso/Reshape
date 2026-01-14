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
    const path = inputPath.value.trim();
    return path.length > 0 && (
        /^[a-zA-Z]:\\/.test(path) ||
        /^\//.test(path) ||
        /^\.\.?\//.test(path)
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
            <span class="input-icon">📁</span>
            <input v-model="inputPath" type="text"
                placeholder="Ordnerpfad eingeben (z.B. C:\Fotos oder /home/user/images)" :disabled="loading"
                @keydown="handleKeydown" class="path-input" />
            <button @click="handleScan" :disabled="!isValidPath || loading" class="scan-btn">
                <span v-if="loading" class="spinner"></span>
                <template v-else>
                    <span class="btn-icon">🔍</span>
                    <span class="btn-text">Scannen</span>
                </template>
            </button>
        </div>
        <p v-if="inputPath && !isValidPath" class="hint error">
            Bitte einen gültigen Ordnerpfad eingeben
        </p>
    </div>
</template>

<style scoped>
.folder-picker {
    margin-bottom: 1.25rem;
}

.input-group {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    background: var(--bg-card, #22242e);
    border: 1px solid var(--border-color, #2e313d);
    border-radius: var(--radius-md, 10px);
    padding: 0.5rem;
    transition: all var(--transition-fast, 150ms ease);
}

.input-group:focus-within {
    border-color: var(--accent-color, #4a90d9);
    box-shadow: 0 0 0 3px var(--accent-bg, rgba(74, 144, 217, 0.12));
}

.input-icon {
    font-size: 1.25rem;
    padding: 0 0.5rem;
}

.path-input {
    flex: 1;
    background: transparent;
    border: none;
    color: var(--text-color, #e8eaed);
    font-size: 0.9rem;
    padding: 0.5rem;
    outline: none;
    font-family: 'JetBrains Mono', 'Consolas', monospace;
}

.path-input::placeholder {
    color: var(--text-dim, #4b5563);
}

.path-input:disabled {
    opacity: 0.6;
}

.scan-btn {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    background: var(--accent-color, #4a90d9);
    color: white;
    border: none;
    border-radius: var(--radius-sm, 6px);
    padding: 0.6rem 1.25rem;
    font-size: 0.875rem;
    font-weight: 500;
    cursor: pointer;
    transition: all var(--transition-fast, 150ms ease);
}

.scan-btn:hover:not(:disabled) {
    background: var(--accent-hover, #5ba0e9);
    transform: translateY(-1px);
    box-shadow: 0 4px 12px rgba(74, 144, 217, 0.3);
}

.scan-btn:disabled {
    opacity: 0.4;
    cursor: not-allowed;
    transform: none;
}

.btn-icon {
    font-size: 1rem;
}

.btn-text {
    font-weight: 600;
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
    margin: 0.5rem 0 0 0.75rem;
    font-size: 0.8rem;
    color: var(--text-muted, #6b7280);
}

.hint.error {
    color: var(--error-color, #f87171);
}
</style>
