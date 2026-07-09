import { ChangeDetectionStrategy, Component, output, signal } from '@angular/core';
import { collectDroppedFiles } from '../../core/upload-filter';

@Component({
  selector: 'app-upload',
  templateUrl: './upload.component.html',
  styleUrl: './upload.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UploadComponent {
  readonly filesSelected = output<File[]>();
  protected readonly dragging = signal(false);

  protected onInputChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files?.length) {
      this.filesSelected.emit(Array.from(input.files));
      input.value = '';
    }
  }

  protected async onDrop(event: DragEvent): Promise<void> {
    event.preventDefault();
    this.dragging.set(false);
    const transfer = event.dataTransfer;
    if (!transfer) {
      return;
    }
    // Snapshot the plain file list synchronously — the DataTransfer is not
    // readable after the handler yields.
    const plainFiles = Array.from(transfer.files);
    const files = transfer.items?.length ? await collectDroppedFiles(transfer.items) : plainFiles;
    const selected = files.length ? files : plainFiles;
    if (selected.length) {
      this.filesSelected.emit(selected);
    }
  }

  protected onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(true);
  }
}
