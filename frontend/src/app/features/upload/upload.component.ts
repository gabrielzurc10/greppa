import { ChangeDetectionStrategy, Component, output, signal } from '@angular/core';

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

  protected onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(false);
    const files = event.dataTransfer?.files;
    if (files?.length) {
      this.filesSelected.emit(Array.from(files));
    }
  }

  protected onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(true);
  }
}
