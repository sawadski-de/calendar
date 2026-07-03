import { Directive, input, output } from '@angular/core';

/**
 * Makes a non-native element (a `div`/`span` standing in for a button) keyboard-activatable and
 * screen-reader-visible as a button: `tabindex="0"`, `role="button"`, and a single `activate` event
 * fired by `Enter`, `Space` (with the browser's default page-scroll-on-Space suppressed — a `div` has
 * no built-in Space handling, unlike a real `<button>`), and by pointer `click` or `dblclick`
 * depending on `activateOn`. Extracted so the same interaction contract isn't hand-rolled per
 * template (`calendar-column`'s slots and appointment blocks, `month-view`'s titles all used to
 * duplicate this — Story 1.4 code review).
 */
@Directive({
  selector: '[appActivatable]',
  standalone: true,
  host: {
    tabindex: '0',
    role: 'button',
    '(click)': 'onClick()',
    '(dblclick)': 'onDblClick()',
    '(keydown.enter)': 'activate.emit()',
    '(keydown.space)': 'onSpace($event)',
  },
})
export class Activatable {
  readonly activateOn = input<'click' | 'dblclick'>('click');
  readonly activate = output<void>();

  onClick(): void {
    if (this.activateOn() === 'click') {
      this.activate.emit();
    }
  }

  onDblClick(): void {
    if (this.activateOn() === 'dblclick') {
      this.activate.emit();
    }
  }

  onSpace(event: Event): void {
    event.preventDefault();
    this.activate.emit();
  }
}
