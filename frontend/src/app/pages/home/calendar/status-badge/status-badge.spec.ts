import { ComponentFixture, TestBed } from '@angular/core/testing';
import { getTranslocoTestingModule } from '../../../../testing/transloco-testing';
import { StatusBadge } from './status-badge';

describe('StatusBadge', () => {
  let fixture: ComponentFixture<StatusBadge>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StatusBadge, getTranslocoTestingModule()],
    }).compileComponents();

    fixture = TestBed.createComponent(StatusBadge);
  });

  it('renders the full, untruncated Unterbrechbar label and icon', () => {
    fixture.componentRef.setInput('status', 'Unterbrechbar');
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('.status-badge--interruptible')).not.toBeNull();
    expect(el.querySelector('.status-badge__icon')).not.toBeNull();
    expect(el.querySelector('.status-badge__label')?.textContent?.trim().length).toBeGreaterThan(0);
  });

  it('renders a visually distinct badge and icon for BitteNichtStoeren', () => {
    fixture.componentRef.setInput('status', 'BitteNichtStoeren');
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('.status-badge--dnd')).not.toBeNull();
    expect(el.querySelector('.status-badge__label')?.textContent?.trim().length).toBeGreaterThan(0);
  });

  it('never truncates the label text', () => {
    fixture.componentRef.setInput('status', 'BitteNichtStoeren');
    fixture.detectChanges();

    const label = fixture.nativeElement.querySelector('.status-badge__label') as HTMLElement;
    expect(getComputedStyle(label).textOverflow).not.toBe('ellipsis');
  });
});
