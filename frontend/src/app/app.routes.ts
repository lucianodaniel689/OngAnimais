import { Routes } from '@angular/router';
import { adminGuard } from '@app/core/auth/admin.guard';
import { authGuard } from '@app/core/auth/auth.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('@app/features/home/home.component').then((m) => m.HomeComponent),
    title: 'OngAnimais — Adote um amigo',
  },
  {
    path: 'animais/:id',
    loadComponent: () =>
      import('@app/features/animais/animal-detalhe/animal-detalhe.component').then(
        (m) => m.AnimalDetalheComponent,
      ),
    title: 'OngAnimais — Detalhes do animal',
  },
  {
    path: 'quiz',
    loadComponent: () =>
      import('@app/features/quiz/quiz.component').then((m) => m.QuizComponent),
    title: 'OngAnimais — Encontre seu match',
  },
  {
    path: 'styleguide',
    loadComponent: () =>
      import('@app/features/styleguide/styleguide.component').then((m) => m.StyleguideComponent),
    title: 'OngAnimais — Styleguide',
  },
  {
    path: 'login',
    loadComponent: () =>
      import('@app/features/auth/login/login.component').then((m) => m.LoginComponent),
    title: 'OngAnimais — Entrar',
  },
  {
    path: 'admin',
    canActivate: [authGuard],
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
        title: 'OngAnimais — Painel',
      },
      {
        path: 'animais',
        loadComponent: () =>
          import('@app/features/admin/animais/animal-lista/animal-lista.component').then(
            (m) => m.AnimalListaComponent,
          ),
        title: 'OngAnimais — Animais cadastrados',
      },
      {
        // A ficha também é a tela de cadastro (modo criação, sem :id).
        path: 'animais/novo',
        loadComponent: () =>
          import('@app/features/admin/animais/animal-ficha/animal-ficha.component').then(
            (m) => m.AnimalFichaComponent,
          ),
        title: 'OngAnimais — Cadastrar animal',
      },
      {
        // Precisa vir depois de 'animais/novo' — ':id' também casaria com "novo".
        // Leitura + edição inline (?editar=1) no mesmo componente.
        path: 'animais/:id',
        loadComponent: () =>
          import('@app/features/admin/animais/animal-ficha/animal-ficha.component').then(
            (m) => m.AnimalFichaComponent,
          ),
        title: 'OngAnimais — Ficha do animal',
      },
      {
        path: 'adocoes',
        loadComponent: () =>
          import('@app/features/admin/adocoes/adocao-lista/adocao-lista.component').then(
            (m) => m.AdocaoListaComponent,
          ),
        title: 'OngAnimais — Adoções',
      },
      {
        path: 'adocoes/nova',
        loadComponent: () =>
          import('@app/features/admin/adocoes/adocao-form/adocao-form.component').then(
            (m) => m.AdocaoFormComponent,
          ),
        title: 'OngAnimais — Nova adoção',
      },
      {
        path: 'vacinas',
        loadComponent: () =>
          import('@app/features/admin/vacinas/vacina-lista/vacina-lista.component').then(
            (m) => m.VacinaListaComponent,
          ),
        title: 'OngAnimais — Vacinas',
      },
      {
        path: 'vacinas/nova',
        loadComponent: () =>
          import('@app/features/admin/vacinas/vacina-form/vacina-form.component').then(
            (m) => m.VacinaFormComponent,
          ),
        title: 'OngAnimais — Registrar vacina',
      },
      {
        path: 'vacinas/:id/editar',
        loadComponent: () =>
          import('@app/features/admin/vacinas/vacina-form/vacina-form.component').then(
            (m) => m.VacinaFormComponent,
          ),
        title: 'OngAnimais — Editar vacina',
      },
      {
        path: 'usuarios',
        canActivate: [adminGuard],
        loadComponent: () =>
          import('@app/features/admin/usuarios/usuario-lista/usuario-lista.component').then(
            (m) => m.UsuarioListaComponent,
          ),
        title: 'OngAnimais — Funcionários',
      },
      {
        path: 'usuarios/novo',
        canActivate: [adminGuard],
        loadComponent: () =>
          import('@app/features/admin/usuarios/usuario-form/usuario-form.component').then(
            (m) => m.UsuarioFormComponent,
          ),
        title: 'OngAnimais — Cadastrar funcionário',
      },
      {
        path: 'usuarios/:id/editar',
        canActivate: [adminGuard],
        loadComponent: () =>
          import('@app/features/admin/usuarios/usuario-form/usuario-form.component').then(
            (m) => m.UsuarioFormComponent,
          ),
        title: 'OngAnimais — Editar funcionário',
      },
      {
        path: 'auditoria',
        canActivate: [adminGuard],
        loadComponent: () =>
          import('@app/features/admin/auditoria/auditoria-lista/auditoria-lista.component').then(
            (m) => m.AuditoriaListaComponent,
          ),
        title: 'OngAnimais — Auditoria',
      },
      {
        path: 'perfil',
        loadComponent: () =>
          import('@app/features/admin/perfil/perfil.component').then((m) => m.PerfilComponent),
        title: 'OngAnimais — Meu perfil',
      },
    ],
  },
  {
    path: '**',
    redirectTo: '',
  },
];
