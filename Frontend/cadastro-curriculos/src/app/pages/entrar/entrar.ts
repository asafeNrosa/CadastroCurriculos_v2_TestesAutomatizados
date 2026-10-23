import { Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { AbstractControl, FormControl, FormGroup, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { mensagemDeErro } from '../../core/errors';
import { AuthService } from '../../core/services/auth.service';
import { MeuCurriculo } from '../../core/services/meu-curriculo';
import { EMAIL_REGEX } from '../../core/validacao';

export const TAMANHO_MINIMO_SENHA = 8;

type Modo = 'entrar' | 'criar';

export function senhasIguais(grupo: AbstractControl): ValidationErrors | null {
  const senha = grupo.get('senha')?.value;
  const confirmacao = grupo.get('confirmarSenha')?.value;
  return senha === confirmacao ? null : { senhasDiferentes: true };
}

function campoEmail() {
  return new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.maxLength(254), Validators.pattern(EMAIL_REGEX)]
  });
}

@Component({
  selector: 'app-entrar',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './entrar.html'
})
export class Entrar {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly meuCurriculo = inject(MeuCurriculo);

  readonly modo = signal<Modo>('entrar');
  readonly enviando = signal(false);
  readonly erro = signal<string | null>(null);

  readonly formEntrar = new FormGroup({
    email: campoEmail(),
    senha: new FormControl('', { nonNullable: true, validators: [Validators.required] })
  });

  readonly formCriar = new FormGroup(
    {
      email: campoEmail(),
      senha: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.minLength(TAMANHO_MINIMO_SENHA), Validators.maxLength(100)]
      }),
      confirmarSenha: new FormControl('', { nonNullable: true, validators: [Validators.required] })
    },
    { validators: senhasIguais }
  );

  alternarModo(modo: Modo): void {
    this.modo.set(modo);
    this.erro.set(null);
  }

  entrar(): void {
    this.erro.set(null);
    if (this.formEntrar.invalid) {
      this.formEntrar.markAllAsTouched();
      return;
    }

    const { email, senha } = this.formEntrar.getRawValue();
    this.enviando.set(true);
    this.auth.entrar({ email: email.trim(), senha })
      .pipe(finalize(() => this.enviando.set(false)))
      .subscribe({
        // Já tem currículo? Vai para ele. Senão, vai para o formulário de cadastro.
        next: () => this.meuCurriculo.abrir(),
        error: (erro: HttpErrorResponse) => this.erro.set(mensagemDeErro(erro, 'Não foi possível entrar.'))
      });
  }

  criarConta(): void {
    this.erro.set(null);
    if (this.formCriar.invalid) {
      this.formCriar.markAllAsTouched();
      return;
    }

    const { email, senha } = this.formCriar.getRawValue();
    this.enviando.set(true);
    this.auth.registrar({ email: email.trim(), senha })
      .pipe(finalize(() => this.enviando.set(false)))
      .subscribe({
        next: () => this.router.navigate(['/candidatos/novo'], { state: { contaCriada: true } }),
        error: (erro: HttpErrorResponse) => this.erro.set(mensagemDeErro(erro, 'Não foi possível criar a conta.'))
      });
  }

  erroDeEmail(controle: FormControl<string>): string | null {
    if (!controle.touched || !controle.errors) return null;
    if (controle.errors['required']) return 'O e-mail é obrigatório.';
    return 'Informe um e-mail válido.';
  }

  erroDeSenha(): string | null {
    const controle = this.formCriar.controls.senha;
    if (!controle.touched || !controle.errors) return null;
    if (controle.errors['required']) return 'A senha é obrigatória.';
    if (controle.errors['minlength']) return `A senha deve ter no mínimo ${TAMANHO_MINIMO_SENHA} caracteres.`;
    return 'A senha deve ter no máximo 100 caracteres.';
  }

  erroDeConfirmacao(): string | null {
    const controle = this.formCriar.controls.confirmarSenha;
    if (!controle.touched) return null;
    if (controle.errors?.['required']) return 'Confirme a senha.';
    if (this.formCriar.errors?.['senhasDiferentes']) return 'As senhas não conferem.';
    return null;
  }
}
