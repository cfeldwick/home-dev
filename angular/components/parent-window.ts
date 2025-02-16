// page.component.ts
import { Component } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { SubmitJobsDialogComponent, SubmitJobsDialogData } from './submit-jobs-dialog.component';

@Component({
  selector: 'app-page',
  template: `
    <button mat-button (click)="openSubmitDialog()">Submit Selected Runs</button>
    <!-- your page content -->
  `
})
export class PageComponent {
  // Assume you have the selected rows available in this.selectedRows
  selectedRows = [ /* ... your row data ... */ ];

  constructor(private dialog: MatDialog) {}

  openSubmitDialog() {
    const dialogData: SubmitJobsDialogData = { selectedRows: this.selectedRows };
    const dialogRef = this.dialog.open(SubmitJobsDialogComponent, {
      width: '400px',
      data: dialogData
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result?.success) {
        // Handle success scenario: maybe refresh data, show a success message, etc.
        console.log('Jobs submitted:', result.results);
      } else {
        // Handle cancellation or failure.
        console.log('Submission cancelled or failed');
      }
    });
  }
}