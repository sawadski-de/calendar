import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Activatable } from './activatable';

@Component({
  standalone: true,
  imports: [Activatable],
  template: `
    <div id="click-target" appActivatable (activate)="clickCount = clickCount + 1"></div>
    <div id="dblclick-target" appActivatable activateOn="dblclick" (activate)="dblClickCount = dblClickCount + 1"></div>
  `,
})
class HostComponent {
  clickCount = 0;
  dblClickCount = 0;
}

describe('Activatable', () => {
  let fixture: ComponentFixture<HostComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [HostComponent] }).compileComponents();
    fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
  });

  function el(id: string): HTMLElement {
    return fixture.nativeElement.querySelector(`#${id}`);
  }

  it('has tabindex="0" and role="button"', () => {
    const target = el('click-target');
    expect(target.getAttribute('tabindex')).toBe('0');
    expect(target.getAttribute('role')).toBe('button');
  });

  it('emits activate on click for the default (click) mode', () => {
    el('click-target').dispatchEvent(new MouseEvent('click', { bubbles: true }));
    expect(fixture.componentInstance.clickCount).toBe(1);
  });

  it('does not emit activate on click when activateOn is dblclick', () => {
    el('dblclick-target').dispatchEvent(new MouseEvent('click', { bubbles: true }));
    expect(fixture.componentInstance.dblClickCount).toBe(0);
  });

  it('emits activate on dblclick when activateOn is dblclick', () => {
    el('dblclick-target').dispatchEvent(new MouseEvent('dblclick', { bubbles: true }));
    expect(fixture.componentInstance.dblClickCount).toBe(1);
  });

  it('emits activate on Enter regardless of activateOn', () => {
    el('click-target').dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
    el('dblclick-target').dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
    expect(fixture.componentInstance.clickCount).toBe(1);
    expect(fixture.componentInstance.dblClickCount).toBe(1);
  });

  it('emits activate on Space and prevents the default page-scroll behavior', () => {
    const event = new KeyboardEvent('keydown', { key: ' ', bubbles: true, cancelable: true });
    el('click-target').dispatchEvent(event);

    expect(fixture.componentInstance.clickCount).toBe(1);
    expect(event.defaultPrevented).toBe(true);
  });
});
