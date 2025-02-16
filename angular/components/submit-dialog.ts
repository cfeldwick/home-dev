// submit-jobs-dialog.component.ts
import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { HttpClient } from '@angular/common/http';
import { from } from 'rxjs';
import { mergeMap, tap, toArray } from 'rxjs/operators';

export interface SubmitJobsDialogData {
  selectedRows: any[];
}

@Component({
  selector: 'app-submit-jobs-dialog',
  template: `
    <h2 mat-dialog-title>Confirm Submission</h2>
    <mat-dialog-content>
      <!-- Confirmation message -->
      <p *ngIf="!isSubmitting">
        You are about to submit {{data.selectedRows.length}} runs. Do you want to continue?
      </p>

      <!-- Progress bar & status while submitting -->
      <div *ngIf="isSubmitting">
        <mat-progress-bar mode="determinate" [value]="progress"></mat-progress-bar>
        <p>{{completedJobs}} / {{totalJobs}} jobs completed</p>
        <mat-spinner *ngIf="!progress && !errorMessage" diameter="40"></mat-spinner>
      </div>

      <!-- Error message display -->
      <div *ngIf="errorMessage" class="error">
        <p>{{ errorMessage }}</p>
      </div>
    </mat-dialog-content>

    <mat-dialog-actions align="end">
      <button mat-button (click)="onCancel()" [disabled]="isSubmitting">Cancel</button>
      <button mat-button color="primary" (click)="onConfirm()" *ngIf="!isSubmitting">
        Submit
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    .error { color: red; }
  `]
})
export class SubmitJobsDialogComponent {
  isSubmitting = false;
  progress = 0;
  completedJobs = 0;
  totalJobs = 0;
  errorMessage = '';

  constructor(
    private dialogRef: MatDialogRef<SubmitJobsDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: SubmitJobsDialogData,
    private http: HttpClient
  ) {}

  onConfirm() {
    this.isSubmitting = true;
    this.totalJobs = this.data.selectedRows.length;

    from(this.data.selectedRows)
      .pipe(
        // Limit concurrency to 3 simultaneous HTTP calls (adjust as needed)
        mergeMap(row => 
          this.http.post('/api/submitJob', row).pipe(
            tap({
              next: () => {
                this.completedJobs++;
                this.progress = (this.completedJobs / this.totalJobs) * 100;
              },
              error: () => {
                // Even if one job fails, update progress and store an error message
                this.completedJobs++;
                this.progress = (this.completedJobs / this.totalJobs) * 100;
                this.errorMessage = 'Some jobs failed to submit.';
              }
            })
          ), 
          3
        ),
        toArray() // Collect all results (optional)
      )
      .subscribe({
        next: results => {
          // All jobs finished; close the dialog with results.
          this.dialogRef.close({ success: true, results });
        },
        error: err => {
          // If the entire stream errors out (unlikely with individual error handling)
          this.errorMessage = 'An error occurred during submission.';
        }
      });
  }

  onCancel() {
    this.dialogRef.close({ success: false });
  }
}