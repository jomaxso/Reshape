// API Types - matching backend models

export interface GpsCoordinates {
    latitude: number;
    longitude: number;
}

export interface FileInfo {
    name: string;
    fullPath: string;
    relativePath: string;
    extension: string;
    size: number;
    createdAt: string;
    modifiedAt: string;
    isSelected: boolean;
    metadata: Record<string, string>;
    gpsCoordinates?: GpsCoordinates;
    dateTakenUtc?: string;
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

// CustomPattern uses the same structure as RenamePattern
export type CustomPattern = RenamePattern;

export interface AddPatternRequest {
    pattern: string;
    description: string;
}

export interface RemovePatternRequest {
    pattern: string;
}

export interface PatternResponse {
    success: boolean;
    message?: string;
}

export interface VacationModeOptions {
    enabled: boolean;
    startDate?: string;
    dayFolderPattern: string;
    subfolderPattern?: string;
}

export interface RenamePreviewRequest {
    folderPath: string;
    pattern: string;
    extensions?: string[];
    vacationMode?: VacationModeOptions;
}

export interface RenamePreviewItem {
    originalName: string;
    newName: string;
    fullPath: string;
    relativePath: string;
    hasConflict: boolean;
    isSelected: boolean;
    dayNumber?: number;
}

export interface RenamePreviewResponse {
    items: RenamePreviewItem[];
    conflictCount: number;
}

export interface RenameExecuteRequest {
    items: RenamePreviewItem[];
    baseFolderPath?: string;
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
