import { ValidatorFn } from '@angular/forms';

export const EMAIL_REGEX = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;

export const TAMANHO_MAXIMO_PDF = 5 * 1024 * 1024; // 5 MB

export const obrigatorio: ValidatorFn = controle =>
  String(controle.value ?? '').trim() ? null : { required: true };