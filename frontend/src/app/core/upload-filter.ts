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

/**
 * Expands drag-and-dropped items into a flat file list. Unlike the file
 * picker, `dataTransfer.files` never expands folders — a dropped directory
 * appears as one zero-byte File — so dropped entries must be walked via
 * webkitGetAsEntry(). Ignored folders are pruned during the walk so dropping
 * a project root doesn't spend minutes reading node_modules.
 */
export async function collectDroppedFiles(items: DataTransferItemList): Promise<File[]> {
  // webkitGetAsEntry() only works while the drop event is live: read every
  // entry synchronously, before the first await.
  const entries = Array.from(items)
    .filter((item) => item.kind === 'file')
    .map((item) => item.webkitGetAsEntry());

  const files: File[] = [];
  for (const entry of entries) {
    if (entry) {
      await walkEntry(entry, files);
    }
  }
  return files;
}

async function walkEntry(entry: FileSystemEntry, out: File[]): Promise<void> {
  if (entry.isFile) {
    try {
      const file = await new Promise<File>((resolve, reject) =>
        (entry as FileSystemFileEntry).file(resolve, reject),
      );
      // Dropped files have an empty webkitRelativePath; recreate it from the
      // entry path so the segment-based ignore rules above still apply.
      Object.defineProperty(file, 'webkitRelativePath', {
        value: entry.fullPath.replace(/^\//, ''),
      });
      out.push(file);
    } catch {
      // Unreadable entry (permissions, vanished file) — skip it.
    }
  } else if (entry.isDirectory) {
    if (IGNORED_SEGMENTS.has(entry.name.toLowerCase())) {
      return;
    }
    const reader = (entry as FileSystemDirectoryEntry).createReader();
    // readEntries() returns at most ~100 entries per call; drain until empty.
    let batch: FileSystemEntry[];
    do {
      batch = await new Promise<FileSystemEntry[]>((resolve, reject) =>
        reader.readEntries(resolve, reject),
      );
      for (const child of batch) {
        await walkEntry(child, out);
      }
    } while (batch.length > 0);
  }
}
