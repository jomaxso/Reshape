import type {
    ScanRequest,
    ScanResponse,
    RenamePattern,
    RenamePreviewRequest,
    RenamePreviewResponse,
    RenameExecuteRequest,
    RenameExecuteResponse,
    VacationModeOptions,
} from './types';

const API_BASE = '/api';

async function request<T>(endpoint: string, options?: RequestInit): Promise<T> {
    const response = await fetch(`${API_BASE}${endpoint}`, {
        headers: {
            'Content-Type': 'application/json',
        },
        ...options,
    });

    if (!response.ok) {
        const error = await response.json().catch(() => ({ error: 'Unknown error' }));
        throw new Error(error.error || `HTTP ${response.status}`);
    }

    return response.json();
}

export const api = {
    /**
     * Scan a folder and get all files with metadata
     */
    async scanFolder(folderPath: string, extensions?: string[]): Promise<ScanResponse> {
        const body: ScanRequest = { folderPath, extensions };
        return request<ScanResponse>('/scan', {
            method: 'POST',
            body: JSON.stringify(body),
        });
    },

    /**
     * Get available rename pattern templates
     */
    async getPatterns(): Promise<RenamePattern[]> {
        return request<RenamePattern[]>('/patterns');
    },

    /**
     * Preview rename operations without executing
     */
    async previewRename(
        folderPath: string,
        pattern: string,
        extensions?: string[],
        vacationMode?: VacationModeOptions
    ): Promise<RenamePreviewResponse> {
        const body: RenamePreviewRequest = {
            folderPath,
            pattern,
            extensions,
            vacationMode
        };
        return request<RenamePreviewResponse>('/preview', {
            method: 'POST',
            body: JSON.stringify(body),
        });
    },

    /**
     * Execute rename operations
     */
    async executeRename(
        items: RenamePreviewRequest['folderPath'] extends string ? RenameExecuteRequest['items'] : never,
        baseFolderPath?: string,
        dryRun = false
    ): Promise<RenameExecuteResponse> {
        const body: RenameExecuteRequest = { items, baseFolderPath, dryRun };
        return request<RenameExecuteResponse>('/rename', {
            method: 'POST',
            body: JSON.stringify(body),
        });
    },

    /**
     * Get metadata for a specific file
     */
    async getFileMetadata(filePath: string): Promise<Record<string, string>> {
        const encodedPath = encodeURIComponent(filePath);
        return request<Record<string, string>>(`/metadata/${encodedPath}`);
    },
};

export default api;
