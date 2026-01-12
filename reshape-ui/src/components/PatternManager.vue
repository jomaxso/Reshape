<template>
    <div class="pattern-manager">
        <div class="manager-header">
            <h4>Musterverwaltung</h4>
            <button @click="showAddDialog = true" class="btn btn-add">
                <span class="btn-icon">➕</span>
                Neues Muster
            </button>
        </div>

        <!-- Add Pattern Dialog -->
        <Transition name="fade">
            <div v-if="showAddDialog" class="dialog-overlay" @click.self="closeAddDialog">
                <div class="dialog">
                    <div class="dialog-header">
                        <h3>Neues Muster hinzufügen</h3>
                        <button @click="closeAddDialog" class="btn-close">✕</button>
                    </div>
                    <div class="dialog-content">
                        <div class="form-group">
                            <label>Muster</label>
                            <input v-model="newPattern" type="text"
                                placeholder="{year}-{month}-{day}_{filename}" class="form-input" />
                        </div>
                        <div class="form-group">
                            <label>Beschreibung</label>
                            <input v-model="newDescription" type="text" placeholder="Beschreibung des Musters"
                                class="form-input" />
                        </div>
                        <div v-if="error" class="error-message">{{ error }}</div>
                    </div>
                    <div class="dialog-actions">
                        <button @click="closeAddDialog" class="btn btn-secondary">Abbrechen</button>
                        <button @click="handleAdd" :disabled="!canAdd" class="btn btn-primary">Hinzufügen</button>
                    </div>
                </div>
            </div>
        </Transition>

        <!-- Patterns List with Delete -->
        <div class="custom-patterns-list">
            <div v-if="customPatterns.length === 0" class="empty-state">
                Keine benutzerdefinierten Muster vorhanden.
            </div>
            <div v-for="pattern in customPatterns" :key="pattern.pattern" class="pattern-row">
                <div class="pattern-info">
                    <code class="pattern-code">{{ pattern.pattern }}</code>
                    <span class="pattern-desc">{{ pattern.description }}</span>
                </div>
                <button @click="handleRemove(pattern.pattern)" class="btn btn-remove" title="Löschen">
                    🗑️
                </button>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import type { RenamePattern } from '../types';

interface Props {
    patterns: RenamePattern[];
    defaultPatternCount: number;
}

interface Emits {
    (e: 'add', pattern: string, description: string): Promise<void>;
    (e: 'remove', pattern: string): Promise<void>;
    (e: 'refresh'): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

const showAddDialog = ref(false);
const newPattern = ref('');
const newDescription = ref('');
const error = ref('');

const customPatterns = computed(() => {
    // Custom patterns are those after the default ones
    return props.patterns.slice(props.defaultPatternCount);
});

const canAdd = computed(() => {
    return newPattern.value.trim() !== '' && newDescription.value.trim() !== '';
});

function closeAddDialog() {
    showAddDialog.value = false;
    newPattern.value = '';
    newDescription.value = '';
    error.value = '';
}

async function handleAdd() {
    if (!canAdd.value) return;

    try {
        error.value = '';
        await emit('add', newPattern.value.trim(), newDescription.value.trim());
        closeAddDialog();
        emit('refresh');
    } catch (e) {
        error.value = e instanceof Error ? e.message : 'Fehler beim Hinzufügen';
    }
}

async function handleRemove(pattern: string) {
    if (!confirm(`Möchten Sie das Muster "${pattern}" wirklich löschen?`)) {
        return;
    }

    try {
        await emit('remove', pattern);
        emit('refresh');
    } catch (e) {
        error.value = e instanceof Error ? e.message : 'Fehler beim Löschen';
    }
}
</script>

<style scoped>
.pattern-manager {
    background: var(--bg-card, #22242e);
    border: 1px solid var(--border-color, #2e313d);
    border-radius: var(--radius-md, 10px);
    padding: 1.25rem;
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.manager-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
}

.manager-header h4 {
    margin: 0;
    font-size: 0.95rem;
    font-weight: 600;
    color: var(--text-color, #e8eaed);
}

.btn {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.5rem 0.875rem;
    border: none;
    border-radius: var(--radius-sm, 6px);
    font-size: 0.8rem;
    font-weight: 500;
    cursor: pointer;
    transition: all var(--transition-fast, 150ms ease);
}

.btn-add {
    background: var(--general-color, #8b5cf6);
    color: white;
}

.btn-add:hover {
    background: #9d6eff;
    transform: translateY(-1px);
}

.btn-icon {
    font-size: 0.9rem;
}

.custom-patterns-list {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.empty-state {
    text-align: center;
    padding: 1.5rem;
    color: var(--text-muted, #6b7280);
    font-size: 0.85rem;
}

.pattern-row {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 0.75rem;
    background: var(--bg-tertiary, #1f2028);
    border: 1px solid var(--border-color, #2e313d);
    border-radius: var(--radius-sm, 6px);
    transition: all var(--transition-fast, 150ms ease);
}

.pattern-row:hover {
    border-color: var(--general-color, #8b5cf6);
    background: var(--bg-hover, #2a2d3a);
}

.pattern-info {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
    flex: 1;
    min-width: 0;
}

.pattern-code {
    font-family: 'JetBrains Mono', 'Consolas', monospace;
    font-size: 0.8rem;
    color: var(--general-color, #8b5cf6);
}

.pattern-desc {
    font-size: 0.75rem;
    color: var(--text-muted, #6b7280);
}

.btn-remove {
    background: transparent;
    color: var(--text-muted, #6b7280);
    padding: 0.4rem 0.6rem;
    border: 1px solid transparent;
}

.btn-remove:hover {
    background: var(--error-bg, rgba(248, 113, 113, 0.12));
    color: var(--error-color, #f87171);
    border-color: var(--error-color, #f87171);
}

/* Dialog */
.dialog-overlay {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background: rgba(0, 0, 0, 0.75);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 1000;
    padding: 1rem;
}

.dialog {
    background: var(--bg-card, #22242e);
    border: 1px solid var(--border-color, #2e313d);
    border-radius: var(--radius-md, 10px);
    max-width: 500px;
    width: 100%;
    box-shadow: 0 10px 40px rgba(0, 0, 0, 0.5);
}

.dialog-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 1.25rem;
    border-bottom: 1px solid var(--border-color, #2e313d);
}

.dialog-header h3 {
    margin: 0;
    font-size: 1.1rem;
    font-weight: 600;
    color: var(--text-color, #e8eaed);
}

.btn-close {
    background: none;
    border: none;
    color: var(--text-muted, #6b7280);
    font-size: 1.5rem;
    cursor: pointer;
    padding: 0;
    width: 2rem;
    height: 2rem;
    display: flex;
    align-items: center;
    justify-content: center;
    border-radius: 4px;
    transition: all var(--transition-fast, 150ms ease);
}

.btn-close:hover {
    background: var(--bg-hover, #2a2d3a);
    color: var(--text-color, #e8eaed);
}

.dialog-content {
    padding: 1.25rem;
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.form-group {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.form-group label {
    font-size: 0.85rem;
    font-weight: 500;
    color: var(--text-secondary, #b8bcc4);
}

.form-input {
    width: 100%;
    padding: 0.65rem 0.875rem;
    background: var(--bg-tertiary, #1f2028);
    border: 1px solid var(--border-color, #2e313d);
    border-radius: var(--radius-sm, 6px);
    color: var(--text-color, #e8eaed);
    font-size: 0.875rem;
    transition: border-color var(--transition-fast, 150ms ease);
}

.form-input:focus {
    outline: none;
    border-color: var(--general-color, #8b5cf6);
}

.error-message {
    padding: 0.75rem;
    background: var(--error-bg, rgba(248, 113, 113, 0.12));
    border: 1px solid var(--error-color, #f87171);
    border-radius: var(--radius-sm, 6px);
    color: var(--error-color, #f87171);
    font-size: 0.85rem;
}

.dialog-actions {
    display: flex;
    gap: 0.75rem;
    padding: 1.25rem;
    border-top: 1px solid var(--border-color, #2e313d);
}

.btn-secondary {
    flex: 1;
    background: var(--bg-tertiary, #1f2028);
    color: var(--text-color, #e8eaed);
    border: 1px solid var(--border-color, #2e313d);
}

.btn-secondary:hover {
    background: var(--bg-hover, #2a2d3a);
    border-color: var(--text-muted, #6b7280);
}

.btn-primary {
    flex: 1;
    background: var(--general-color, #8b5cf6);
    color: white;
}

.btn-primary:hover:not(:disabled) {
    background: #9d6eff;
    transform: translateY(-1px);
}

.btn-primary:disabled {
    opacity: 0.4;
    cursor: not-allowed;
}

/* Fade transition */
.fade-enter-active,
.fade-leave-active {
    transition: opacity var(--transition-normal, 250ms ease);
}

.fade-enter-from,
.fade-leave-to {
    opacity: 0;
}
</style>
