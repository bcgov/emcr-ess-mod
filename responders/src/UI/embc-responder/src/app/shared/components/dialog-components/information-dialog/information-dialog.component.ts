import { Component, EventEmitter, Input, Output } from '@angular/core';
import { DialogContent } from 'src/app/core/models/dialog-content.model';
import { MatButton } from '@angular/material/button';
import * as globalConst from '../../../../core/services/global-constants';
import { StepNotesService } from '../../../../feature-components/wizard/step-notes/step-notes.service';
@Component({
  selector: 'app-information-dialog',
  templateUrl: './information-dialog.component.html',
  styleUrls: ['./information-dialog.component.scss'],
  standalone: true,
  imports: [MatButton]
})
export class InformationDialogComponent {
  @Input() content: DialogContent;
  @Output() outputEvent = new EventEmitter<string>();

  constructor(private stepNotesService: StepNotesService) {}

  cancel(): void {
    this.outputEvent.emit('cancel');
  }

  confirm(): void {
    if (this.content?.confirmButton === globalConst.DuplicateSupportPopupProceedMessage) {
      this.createNote(globalConst.confirmDuplicateSupportMessage);
    }
    this.outputEvent.emit('confirm');
  }

  exit(): void {
    this.outputEvent.emit('exit');
  }

  createNote(noteContent: string | null): void {
    this.stepNotesService
      .saveNotes(
        this.stepNotesService.createNoteDTO({
          note: noteContent,
          id: undefined,
          isImportant: true
        })
      )
      .subscribe({
        next: (result) => {
          // success
        },
        error: (error) => {
          // error
        }
      });
  }
}
