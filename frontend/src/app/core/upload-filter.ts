const MAX_TOTAL_BYTES = 50 * 1024 * 1024;
const MAX_FILE_BYTES = 1024 * 1024;
const MAX_FILE_COUNT = 200;

const IGNORED_SEGMENTS = new Set(['node_modules', '.git', '.angular', 'dist', 'bin', 'obj']);
const IGNORED_FILE_NAMES = new Set([
  'package-lock.json',
  'yarn.lock',
  'pnpm-lock.yaml',
  'poetry.lock',
  'cargo.lock',
  '.ds_store',
]);

export interface UploadSelection {
  files: File[];
  error: string | null;
}

/**
 * Mirrors the server-side upload filters (UploadOptions.cs) so dependency
 * folders and oversized files are dropped before anything goes over the wire —
 * uploading them just to have the server skip them can take minutes for a
 * folder containing node_modules.
 */
export function selectScannableFiles(files: File[]): UploadSelection {
  const scannable = files.filter((file) => {
    const path = ((file as { webkitRelativePath?: string }).webkitRelativePath || file.name)
      .replace(/\\/g, '/');
    const segments = path.split('/');
    if (segments.some((s) => IGNORED_SEGMENTS.has(s.toLowerCase()))) {
      return false;
    }
    if (IGNORED_FILE_NAMES.has(segments[segments.length - 1].toLowerCase())) {
      return false;
    }
    return file.size > 0 && file.size <= MAX_FILE_BYTES;
  });

  if (scannable.length === 0) {
    return {
      files: [],
      error:
        'No scannable files found — everything selected was empty, larger than 1 MB, or inside a skipped folder like node_modules.',
    };
  }

  if (scannable.length > MAX_FILE_COUNT) {
    return {
      files: [],
      error: `Too many files: ${scannable.length} after skipping dependency folders (the limit is ${MAX_FILE_COUNT}). Try a subfolder instead.`,
    };
  }

  const totalBytes = scannable.reduce((sum, f) => sum + f.size, 0);
  if (totalBytes > MAX_TOTAL_BYTES) {
    return {
      files: [],
      error:
        'The selection is over the 50 MB limit even after skipping dependency folders. Try a smaller folder.',
    };
  }

  return { files: scannable, error: null };
}
