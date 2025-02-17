import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { SubmitJobsDialogComponent, SubmitJobsDialogData } from './submit-dialog';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatButtonModule } from '@angular/material/button';
import { MatSpinner } from '@angular/material/progress-spinner';
import { By } from '@angular/platform-browser';

describe('SubmitJobsDialogComponent', () => {
  let component: SubmitJobsDialogComponent;
  let fixture: ComponentFixture<SubmitJobsDialogComponent>;
  let httpMock: HttpTestingController;
  let dialogRefSpy: jasmine.SpyObj<MatDialogRef<SubmitJobsDialogComponent>>;

  beforeEach(waitForAsync(() => {
    const dialogData: SubmitJobsDialogData = { selectedRows: [{ id: 1 }, { id: 2 }, { id: 3 }] };
    dialogRefSpy = jasmine.createSpyObj('MatDialogRef', ['close']);

    TestBed.configureTestingModule({
      declarations: [SubmitJobsDialogComponent, MatSpinner],
      imports: [
        HttpClientTestingModule,
        NoopAnimationsModule,
        MatProgressBarModule,
        MatButtonModule
      ],
      providers: [
        { provide: MAT_DIALOG_DATA, useValue: dialogData },
        { provide: MatDialogRef, useValue: dialogRefSpy }
      ]
    }).compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(SubmitJobsDialogComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should display the correct number of jobs to be submitted', () => {
    const content = fixture.debugElement.query(By.css('mat-dialog-content p')).nativeElement;
    expect(content.textContent).toContain('You are about to submit 3 runs. Do you want to continue?');
  });

  it('should start submitting jobs on confirm', () => {
    component.onConfirm();
    expect(component.isSubmitting).toBeTrue();
    expect(component.totalJobs).toBe(3);
    const reqs = httpMock.match('/api/submitJob');
    expect(reqs.length).toBe(3);
    reqs.forEach(req => req.flush({}));
  });

  it('should update progress and complete jobs', () => {
    component.onConfirm();
    const reqs = httpMock.match('/api/submitJob');
    reqs.forEach(req => req.flush({}));

    expect(component.completedJobs).toBe(3);
    expect(component.progress).toBe(100);
    expect(dialogRefSpy.close).toHaveBeenCalledWith({ success: true, results: [{}, {}, {}] });
  });

  it('should handle errors during job submission', () => {
    component.onConfirm();
    const reqs = httpMock.match('/api/submitJob');
    reqs[0].flush({}, { status: 500, statusText: 'Internal Server Error' });
    reqs[1].flush({});
    reqs[2].flush({});

    expect(component.completedJobs).toBe(3);
    expect(component.progress).toBe(100);
    expect(component.errorMessage).toBe('Some jobs failed to submit.');
  });

  it('should close the dialog on cancel', () => {
    component.onCancel();
    expect(dialogRefSpy.close).toHaveBeenCalledWith({ success: false });
  });
});