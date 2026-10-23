import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './core/services/auth.service';
import { MeuCurriculo } from './core/services/meu-curriculo';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected readonly auth = inject(AuthService);
  private readonly meuCurriculo = inject(MeuCurriculo);
  private readonly router = inject(Router);

  abrirMeuCurriculo(): void {
    this.meuCurriculo.abrir();
  }

  sair(): void {
    this.auth.sair();
    this.router.navigate(['/']);
  }
}
