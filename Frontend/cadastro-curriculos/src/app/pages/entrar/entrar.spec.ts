import { FormControl, FormGroup } from '@angular/forms';
import { senhasIguais } from './entrar';

describe('senhasIguais', () => {
  const grupo = (senha: string, confirmarSenha: string) =>
    new FormGroup({ senha: new FormControl(senha), confirmarSenha: new FormControl(confirmarSenha) });

  it('aceita quando a confirmação é igual à senha', () => {
    expect(senhasIguais(grupo('senha-segura-123', 'senha-segura-123'))).toBeNull();
  });

  it('rejeita quando as senhas são diferentes', () => {
    expect(senhasIguais(grupo('senha-segura-123', 'senha-segura-124'))).toEqual({ senhasDiferentes: true });
  });

  it('diferencia maiúsculas de minúsculas', () => {
    expect(senhasIguais(grupo('Senha', 'senha'))).toEqual({ senhasDiferentes: true });
  });
});
