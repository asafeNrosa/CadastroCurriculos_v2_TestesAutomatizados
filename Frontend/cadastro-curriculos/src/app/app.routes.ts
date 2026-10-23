import { Routes } from '@angular/router';
import { Inicio } from './pages/inicio/inicio';
import { CandidatoForm } from './pages/candidato-form/candidato-form';
import { CandidatoLista } from './pages/candidato-lista/candidato-lista';
import { CandidatoDetalhe } from './pages/candidato-detalhe/candidato-detalhe';
import { Entrar } from './pages/entrar/entrar';

export const routes: Routes = [
  { path: '', component: Inicio, title: 'Cadastro de Currículos' },
  { path: 'entrar', component: Entrar, title: 'Entrar' },
  { path: 'candidatos', component: CandidatoLista, title: 'Candidatos' },
  { path: 'candidatos/novo', component: CandidatoForm, title: 'Novo cadastro' },
  { path: 'candidatos/:id', component: CandidatoDetalhe, title: 'Detalhes do candidato' },
  { path: '**', redirectTo: '' }
];