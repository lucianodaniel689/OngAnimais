import { Routes } from '@angular/router';
import { provideNativeDateAdapter } from '@angular/material/core';
import { adminGuard } from '@app/core/auth/admin.guard';
import { authGuard, loginGuard } from '@app/core/auth/auth.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('@app/features/home/home.component').then((m) => m.HomeComponent),
    title: 'Adote um amigo',
  },
  {
    path: 'adotar',
    loadComponent: () =>
      import('@app/features/animais/animais-disponiveis/animais-disponiveis.component').then(
        (m) => m.AnimaisDisponiveisComponent,
      ),
    title: 'Quero adotar',
  },
  {
    path: 'animais/:id',
    loadComponent: () =>
      import('@app/features/animais/animal-detalhe/animal-detalhe.component').then(
        (m) => m.AnimalDetalheComponent,
      ),
    title: 'Detalhes do animal',
  },
  {
    path: 'styleguide',
    // Só essa rota (fora do /admin) usa mat-datepicker no catálogo — escopado
    // aqui em vez de global em app.config.ts pra não pesar no bundle inicial
    // do site público, que nunca usa datepicker.
    providers: [provideNativeDateAdapter()],
    loadComponent: () =>
      import('@app/features/styleguide/styleguide.component').then((m) => m.StyleguideComponent),
    title: 'Styleguide',
  },
  {
    path: 'login',
    canActivate: [loginGuard],
    loadComponent: () =>
      import('@app/features/auth/login/login.component').then((m) => m.LoginComponent),
    title: 'Entrar',
  },
  {
    path: 'admin',
    canActivate: [authGuard],
    // Datepicker (Vacinas, Auditoria) só existe aqui dentro — escopado no
    // /admin em vez de global, pra não pesar no bundle inicial do site público.
    providers: [provideNativeDateAdapter()],
    loadComponent: () =>
      import('@app/features/admin/admin-layout/admin-layout.component').then(
        (m) => m.AdminLayoutComponent,
      ),
    children: [
      {
        path: '',
        loadComponent: () =>
          import('@app/features/admin/dashboard/dashboard.component').then(
            (m) => m.DashboardComponent,
          ),
        title: 'Painel',
      },
      {
        path: 'animais',
        loadComponent: () =>
          import('@app/features/admin/animais/animal-lista/animal-lista.component').then(
            (m) => m.AnimalListaComponent,
          ),
        title: 'Animais cadastrados',
      },
      {
        // A ficha também é a tela de cadastro (modo criação, sem :id).
        path: 'animais/novo',
        loadComponent: () =>
          import('@app/features/admin/animais/animal-ficha/animal-ficha.component').then(
            (m) => m.AnimalFichaComponent,
          ),
        title: 'Cadastrar animal',
      },
      {
        // Precisa vir depois de 'animais/novo' — ':id' também casaria com "novo".
        // Leitura + edição inline (?editar=1) no mesmo componente.
        path: 'animais/:id',
        loadComponent: () =>
          import('@app/features/admin/animais/animal-ficha/animal-ficha.component').then(
            (m) => m.AnimalFichaComponent,
          ),
        title: 'Ficha do animal',
      },
      {
        path: 'adocoes',
        loadComponent: () =>
          import('@app/features/admin/adocoes/adocao-lista/adocao-lista.component').then(
            (m) => m.AdocaoListaComponent,
          ),
        title: 'Adoções',
      },
      {
        path: 'adocoes/nova',
        loadComponent: () =>
          import('@app/features/admin/adocoes/adocao-form/adocao-form.component').then(
            (m) => m.AdocaoFormComponent,
          ),
        title: 'Nova adoção',
      },
      {
        path: 'adocoes/:id',
        loadComponent: () =>
          import('@app/features/admin/adocoes/adocao-ficha/adocao-ficha.component').then(
            (m) => m.AdocaoFichaComponent,
          ),
        title: 'Ficha da candidatura',
      },
      {
        path: 'vacinas/nova',
        loadComponent: () =>
          import('@app/features/admin/vacinas/vacina-form/vacina-form.component').then(
            (m) => m.VacinaFormComponent,
          ),
        title: 'Registrar vacina',
      },
      {
        path: 'vacinas/:id/editar',
        loadComponent: () =>
          import('@app/features/admin/vacinas/vacina-form/vacina-form.component').then(
            (m) => m.VacinaFormComponent,
          ),
        title: 'Editar vacina',
      },
      {
        path: 'usuarios',
        canActivate: [adminGuard],
        loadComponent: () =>
          import('@app/features/admin/usuarios/usuario-lista/usuario-lista.component').then(
            (m) => m.UsuarioListaComponent,
          ),
        title: 'Funcionários',
      },
      {
        path: 'usuarios/novo',
        canActivate: [adminGuard],
        loadComponent: () =>
          import('@app/features/admin/usuarios/usuario-form/usuario-form.component').then(
            (m) => m.UsuarioFormComponent,
          ),
        title: 'Cadastrar funcionário',
      },
      {
        path: 'auditoria',
        canActivate: [adminGuard],
        loadComponent: () =>
          import('@app/features/admin/auditoria/auditoria-lista/auditoria-lista.component').then(
            (m) => m.AuditoriaListaComponent,
          ),
        title: 'Auditoria',
      },
      {
        path: 'configuracoes',
        canActivate: [adminGuard],
        loadComponent: () =>
          import('@app/features/admin/configuracoes/configuracoes.component').then(
            (m) => m.ConfiguracoesComponent,
          ),
        title: 'Configurações',
      },
      {
        path: 'perfil',
        loadComponent: () =>
          import('@app/features/admin/perfil/perfil.component').then((m) => m.PerfilComponent),
        title: 'Meu perfil',
      },
    ],
  },
  {
    // Destino dos guards (authGuard/adminGuard) quando falta acesso — renderizado com
    // skipLocationChange, então a barra continua mostrando a URL que a pessoa digitou.
    path: 'nao-encontrado',
    loadComponent: () =>
      import('@app/features/nao-encontrado/nao-encontrado.component').then(
        (m) => m.NaoEncontradoComponent,
      ),
    title: 'Página não encontrada',
  },
  {
    // URL desconhecida → mesma página 404 (antes redirecionava para a home). Precisa ser
    // idêntica à resposta dos guards: se URLs inválidas fossem para a home e só /admin desse
    // 404, a diferença denunciaria que a área restrita existe.
    path: '**',
    loadComponent: () =>
      import('@app/features/nao-encontrado/nao-encontrado.component').then(
        (m) => m.NaoEncontradoComponent,
      ),
    title: 'Página não encontrada',
  },
];
