import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { catchError, map, of } from 'rxjs';
import { AnimalRespostaDto, AnimalService, EspecieAnimal, PorteAnimal, SexoAnimal } from '@app/core/api';
import { ROTULO_ESPECIE, ROTULO_PORTE, ROTULO_SEXO } from '@app/core/models';
import { AnimalCardComponent, LoadingComponent } from '@app/shared/components';

/** Quantidade de cards exibidos antes de "Mostrar mais". */
const QUANTIDADE_INICIAL = 12;

/**
 * Página pública "Quero adotar" (`/adotar`): todos os animais disponíveis, com
 * filtros por espécie/porte/sexo. Extraída da home — lá fica só o carrossel de
 * destaques + o CTA que traz pra cá, sem competir por espaço com o resto do
 * conteúdo institucional (como adotar, formas de ajudar, contato).
 */
@Component({
  selector: 'app-animais-disponiveis',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatIconModule, MatChipsModule, AnimalCardComponent, LoadingComponent],
  templateUrl: './animais-disponiveis.component.html',
  styleUrl: './animais-disponiveis.component.scss',
})
export class AnimaisDisponiveisComponent {
  private animalService = inject(AnimalService);

  readonly animais = signal<AnimalRespostaDto[]>([]);
  readonly carregando = signal(true);
  readonly erro = signal<string | null>(null);

  readonly especieSelecionada = signal<EspecieAnimal | null>(null);
  readonly porteSelecionado = signal<PorteAnimal | null>(null);
  readonly sexoSelecionado = signal<SexoAnimal | null>(null);
  readonly mostrarTodos = signal(false);

  readonly rotuloEspecie = ROTULO_ESPECIE;
  readonly rotuloPorte = ROTULO_PORTE;
  readonly rotuloSexo = ROTULO_SEXO;

  /** Espécies presentes na lista carregada, na ordem em que aparecem. */
  readonly especiesDisponiveis = computed(() => {
    const vistas = new Set<EspecieAnimal>();
    for (const a of this.animais()) {
      if (a.especie) {
        vistas.add(a.especie);
      }
    }
    return Array.from(vistas);
  });

  /** Portes presentes na lista carregada — o filtro só aparece quando há variedade. */
  readonly portesDisponiveis = computed(() => {
    const vistos = new Set<PorteAnimal>();
    for (const a of this.animais()) {
      if (a.porte) {
        vistos.add(a.porte);
      }
    }
    return Array.from(vistos);
  });

  /** Sexos presentes na lista carregada — o filtro só aparece quando há variedade. */
  readonly sexosDisponiveis = computed(() => {
    const vistos = new Set<SexoAnimal>();
    for (const a of this.animais()) {
      if (a.sexo) {
        vistos.add(a.sexo);
      }
    }
    return Array.from(vistos);
  });

  readonly animaisFiltrados = computed(() => {
    const especie = this.especieSelecionada();
    const porte = this.porteSelecionado();
    const sexo = this.sexoSelecionado();
    return this.animais().filter(
      (a) =>
        (!especie || a.especie === especie) &&
        (!porte || a.porte === porte) &&
        (!sexo || a.sexo === sexo),
    );
  });

  readonly animaisExibidos = computed(() => {
    const lista = this.animaisFiltrados();
    return this.mostrarTodos() ? lista : lista.slice(0, QUANTIDADE_INICIAL);
  });

  readonly temMaisParaMostrar = computed(
    () => !this.mostrarTodos() && this.animaisFiltrados().length > QUANTIDADE_INICIAL,
  );

  constructor() {
    this.carregarDisponiveis();
  }

  filtrarPorEspecie(especie: EspecieAnimal | null): void {
    this.especieSelecionada.set(especie);
    this.mostrarTodos.set(false);
  }

  filtrarPorPorte(porte: PorteAnimal | null): void {
    this.porteSelecionado.set(porte);
    this.mostrarTodos.set(false);
  }

  filtrarPorSexo(sexo: SexoAnimal | null): void {
    this.sexoSelecionado.set(sexo);
    this.mostrarTodos.set(false);
  }

  mostrarMais(): void {
    this.mostrarTodos.set(true);
  }

  private carregarDisponiveis(): void {
    this.carregando.set(true);
    this.animalService
      .animalListarDisponiveis()
      .pipe(
        map((r) => r.dados ?? []),
        catchError(() => {
          this.erro.set('Não foi possível carregar os animais agora.');
          return of([] as AnimalRespostaDto[]);
        }),
      )
      .subscribe((lista) => {
        this.animais.set(lista);
        this.carregando.set(false);
      });
  }
}
