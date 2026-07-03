import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FocusTrap } from './focus-trap';

@Component({
  standalone: true,
  imports: [FocusTrap],
  template: `
    <button id="outside">Outside</button>
    @if (trapOpen()) {
      <div appFocusTrap>
        <button id="first">First</button>
        <button id="last">Last</button>
      </div>
    }
  `,
})
class HostComponent {
  readonly trapOpen = signal(false);
}

describe('FocusTrap', () => {
  let fixture: ComponentFixture<HostComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [HostComponent] }).compileComponents();
    fixture = TestBed.createComponent(HostComponent);
    document.body.appendChild(fixture.nativeElement);
  });

  afterEach(() => fixture.nativeElement.remove());

  it('wraps Tab from the last focusable element back to the first', () => {
    (fixture.nativeElement.querySelector('#outside') as HTMLElement).focus();
    fixture.componentInstance.trapOpen.set(true);
    fixture.detectChanges();

    const last = fixture.nativeElement.querySelector('#last') as HTMLElement;
    last.focus();
    last.dispatchEvent(new KeyboardEvent('keydown', { key: 'Tab', bubbles: true }));

    expect(document.activeElement?.id).toBe('first');
  });

  it('wraps Shift+Tab from the first focusable element back to the last', () => {
    (fixture.nativeElement.querySelector('#outside') as HTMLElement).focus();
    fixture.componentInstance.trapOpen.set(true);
    fixture.detectChanges();

    const first = fixture.nativeElement.querySelector('#first') as HTMLElement;
    first.focus();
    first.dispatchEvent(new KeyboardEvent('keydown', { key: 'Tab', shiftKey: true, bubbles: true }));

    expect(document.activeElement?.id).toBe('last');
  });

  it('restores focus to the pre-trap active element when deactivated', () => {
    const outside = fixture.nativeElement.querySelector('#outside') as HTMLElement;
    outside.focus();
    fixture.componentInstance.trapOpen.set(true);
    fixture.detectChanges();

    fixture.componentInstance.trapOpen.set(false);
    fixture.detectChanges();

    expect(document.activeElement).toBe(outside);
  });
});
