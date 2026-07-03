import { AfterViewInit, Directive, ElementRef, OnDestroy, inject } from '@angular/core';

const FOCUSABLE_SELECTOR =
  'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';

/**
 * Traps `Tab`/`Shift+Tab` cycling within its host while attached, and restores focus to whatever was
 * active before it was applied when removed. Apply to a popover/dialog-like container that is only
 * ever inserted into the DOM while "open" (e.g. behind an `@if`) — this is the app's first popover-like
 * UI (Story 1.3's create form); Story 1.4's detail popover reuses this rather than duplicating it.
 */
@Directive({
  selector: '[appFocusTrap]',
  standalone: true,
  host: {
    '(keydown.tab)': 'onTab($event, false)',
    '(keydown.shift.tab)': 'onTab($event, true)',
  },
})
export class FocusTrap implements AfterViewInit, OnDestroy {
  private readonly elementRef: ElementRef<HTMLElement> = inject(ElementRef);
  private previouslyFocused: HTMLElement | null = null;

  ngAfterViewInit(): void {
    this.previouslyFocused = document.activeElement as HTMLElement | null;
    this.focusableElements()[0]?.focus();
  }

  ngOnDestroy(): void {
    this.previouslyFocused?.focus();
  }

  onTab(event: Event, shift: boolean): void {
    const focusable = this.focusableElements();
    if (focusable.length === 0) {
      return;
    }

    const first = focusable[0];
    const last = focusable[focusable.length - 1];
    const active = document.activeElement;

    if (!shift && active === last) {
      event.preventDefault();
      first.focus();
    } else if (shift && active === first) {
      event.preventDefault();
      last.focus();
    }
  }

  private focusableElements(): HTMLElement[] {
    const nativeElement: HTMLElement = this.elementRef.nativeElement;
    return Array.from(nativeElement.querySelectorAll(FOCUSABLE_SELECTOR));
  }
}
