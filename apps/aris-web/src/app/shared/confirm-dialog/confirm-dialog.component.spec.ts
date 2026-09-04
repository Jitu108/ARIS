import { TestBed } from '@angular/core/testing';
import { ConfirmDialogComponent } from './confirm-dialog.component';

describe('ConfirmDialogComponent', () => {
  function createComponent() {
    const fixture = TestBed.createComponent(ConfirmDialogComponent);
    fixture.componentInstance.title = 'Deactivate Jane Doe?';
    fixture.componentInstance.message = "They'll be signed out immediately and won't be able to log in until reactivated.";
    fixture.componentInstance.confirmLabel = 'Deactivate';
    fixture.detectChanges();
    return fixture;
  }

  it('renders the given title, message, and confirm label', () => {
    const fixture = createComponent();

    expect(fixture.nativeElement.textContent).toContain('Deactivate Jane Doe?');
    expect(fixture.nativeElement.textContent).toContain("won't be able to log in until reactivated.");
    expect(fixture.nativeElement.textContent).toContain('Deactivate');
  });

  it('emits confirm when the confirm button is clicked', () => {
    const fixture = createComponent();
    const emitted = vi.fn();
    fixture.componentInstance.confirm.subscribe(emitted);

    fixture.nativeElement.querySelector('.confirm-button').click();

    expect(emitted).toHaveBeenCalledOnce();
  });

  it('emits cancel when the cancel button or backdrop is clicked', () => {
    const fixture = createComponent();
    const emitted = vi.fn();
    fixture.componentInstance.cancel.subscribe(emitted);

    fixture.nativeElement.querySelector('.cancel-button').click();

    expect(emitted).toHaveBeenCalledOnce();
  });

  it('disables both buttons while confirming', () => {
    const fixture = createComponent();
    fixture.componentRef.setInput('confirming', true);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.confirm-button').disabled).toBe(true);
    expect(fixture.nativeElement.querySelector('.cancel-button').disabled).toBe(true);
  });
});
