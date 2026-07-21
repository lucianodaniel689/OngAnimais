import { Directive, TemplateRef, inject } from '@angular/core';

/**
 * Marca um `<ng-template appCarrosselSlide>` como um slide do carrossel.
 * O `CarrosselComponent` coleta esses templates e os renderiza/controla.
 */
@Directive({
  selector: '[appCarrosselSlide]',
  standalone: true,
})
export class CarrosselSlideDirective {
  readonly template = inject(TemplateRef<unknown>);
}
