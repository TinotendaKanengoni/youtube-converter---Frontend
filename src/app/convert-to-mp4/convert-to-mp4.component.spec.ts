import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ConvertToMp4Component } from './convert-to-mp4.component';

describe('ConvertToMp4Component', () => {
  let component: ConvertToMp4Component;
  let fixture: ComponentFixture<ConvertToMp4Component>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ConvertToMp4Component]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(ConvertToMp4Component);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
