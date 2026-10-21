# User.Games.Fiap.Tests

Testes unitários do UsersAPI, rodados pelo passo **Build & Test** da pipeline (`dotnet test User.Games.Fiap.slnx`).

Fica na raiz do repositório, fora de `User.Games.Fiap/`. Se ficasse dentro, os `.cs` dos testes entrariam na compilação da API e no contexto do `docker build`.

**Esta é só a base**, criada para a pipeline da Fase 4 ter o que executar. Hoje cobre apenas os validators de entrada.

## Coberto

- `CreateUserRequestValidator`: nome, e-mail e as regras de senha (tamanho, maiúscula, minúscula, número)
- `LoginRequestValidator`: e-mail e senha obrigatórios

## Pendente

Para as classes com dependências, use mocks. Sugestão: NSubstitute.

- [ ] `RefreshTokenRequestValidator`
- [ ] `UserService`: e-mail duplicado, senha gravada como hash, paginação
- [ ] `AuthService`: login com senha errada, claims do JWT, rotação e expiração do refresh token
- [ ] `UserLookupRequestedConsumer`: usuário encontrado e não encontrado
- [ ] `NotificationsClient`: falha na Azure Function vira `LogWarning` e não derruba o cadastro
- [ ] `UserMappingProfile`: `AssertConfigurationIsValid` do AutoMapper
