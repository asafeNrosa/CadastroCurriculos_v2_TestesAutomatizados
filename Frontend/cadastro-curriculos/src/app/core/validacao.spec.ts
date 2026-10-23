import { FormControl } from '@angular/forms';
import { EMAIL_REGEX, obrigatorio } from './validacao';

describe('validacao', () => {
  describe('obrigatorio', () => {
    it.each(['', '   ', '\t\n'])('rejeita texto vazio ou só com espaços (%j)', valor => {
      expect(obrigatorio(new FormControl(valor))).toEqual({ required: true });
    });

    it('rejeita null e undefined', () => {
      expect(obrigatorio(new FormControl(null))).toEqual({ required: true });
      expect(obrigatorio(new FormControl(undefined))).toEqual({ required: true });
    });

    it('aceita texto com conteúdo, mesmo com espaços nas pontas', () => {
      expect(obrigatorio(new FormControl('  Maria  '))).toBeNull();
    });
  });

  describe('EMAIL_REGEX', () => {
    it.each(['maria@exemplo.com', 'a.b+c@dominio.com.br'])('aceita %s', email => {
      expect(EMAIL_REGEX.test(email)).toBe(true);
    });

    it.each(['sem-arroba', '@semlocal.com', 'sem@dominio', 'com espaco@x.com', ''])('rejeita %j', email => {
      expect(EMAIL_REGEX.test(email)).toBe(false);
    });
  });
});
