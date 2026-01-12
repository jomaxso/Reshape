// API Types - matching backend models

export interface FileInfo {
    name: string;
    fullPath: string;
    extension: string;
    size: number;
    createdAt: string;
    modifiedAt: string;
    metadata: Record<string, string>;
}

export interface ScanRequest {
    folderPath: string;
    extensions?: string[];
}

export interface ScanResponse {
    folderPath: string;
    files: FileInfo[];
    totalCount: number;
}

export interface RenamePattern {
    pattern: string;
    description: string;
}

export interface RenamePreviewRequest {
    folderPath: string;
    pattern: string;
    extensions?: string[];
}

export interface RenamePreviewItem {
    originalName: string;
    newName: string;
    fullPath: string;
    hasConflict: boolean;
}

export interface RenamePreviewResponse {
    items: RenamePreviewItem[];
    conflictCount: number;
}

export interface RenameExecuteRequest {
    items: RenamePreviewItem[];
    dryRun?: boolean;
}

export interface RenameResult {
    originalPath: string;
    newPath: string;
    success: boolean;
    error?: string;
}

export interface RenameExecuteResponse {
    results: RenameResult[];
    successCount: number;
    errorCount: number;
}

export interface ApiError {
    error: string;
}
