# ⚡ ProspectAI - Plataforma de Prospecção B2B

> Sistema completo e moderno para busca, gestão, qualificação e prospecção de leads e estabelecimentos comerciais em todo o Brasil.

---

## 🌟 Funcionalidades Principais

- 🔍 **Busca de estabelecimentos reais**:
  - Integração com **OpenStreetMap / Overpass API** com múltiplos espelhos de contingência.
  - Dados não encontrados permanecem vazios; contatos e avaliações não são estimados.
  - Suporte opcional a Apify e Google Places quando suas chaves forem configuradas.
- 🎯 **Gestão e Funil de Leads (CRM)**:
  - Visualização em tabela com busca em tempo real, ordenação dinâmica e filtros por status (*Novo, Contatado, Em Negociação, Fechado, Perdido*).
  - Modal interativo com ações rápidas de contato: **WhatsApp direto**, **Ligação telefônica**, **E-mail**, **Instagram** e rota no **Google Maps**.
  - Registro de anotações internas e histórico de contato por lead.
- 📊 **Dashboard & KPIs em Tempo Real**:
  - Métricas de leads totais, taxa de conversão, leads contatados e distribuição por nicho/cidade.
- 📥 **Exportação de Dados**:
  - Exportação em formato **Excel (.xlsx)** e **CSV** estruturado.
- 🔐 **Autenticação & Segurança**:
  - Cadastro, confirmação de e-mail, login, logout e recuperação de senha com Supabase Auth.
  - Login com Google via OAuth 2.0 + PKCE.
  - Renovação automática de sessão e armazenamento protegido no navegador.
  - Login local reservado ao desenvolvimento, sem serviços pagos.
  - Suporte a banco de dados **PostgreSQL (Supabase)** ou **SQLite** local para desenvolvimento.

---

## 🏗️ Arquitetura e Tecnologias

- **Frontend & Backend**: [.NET 10](https://dotnet.microsoft.com/) com Blazor Server interativo.
- **Banco de Dados**: Entity Framework Core com PostgreSQL (`Npgsql`) e SQLite.
- **Autenticação**: modo local individual ou Supabase Auth.
- **Design & UI**: CSS customizado com tema Dark moderno, Glassmorphism, badges responsivos e feedback via toasts dinâmicos.

```
src/
 ├── ProspeccaoLeads.Domain/         # Entidades, Enums e Interfaces centrais
 ├── ProspeccaoLeads.Application/    # DTOs, Serviços de Domínio e Casos de Uso
 ├── ProspeccaoLeads.Infrastructure/ # EF Core, Supabase Auth, Overpass OSM, Exportação
 └── ProspeccaoLeads.Web/            # Componentes Blazor, Páginas, Layouts e UI
tests/
 └── ProspeccaoLeads.Tests/          # Bateria de testes unitários e de integração
```

---

## 🚀 Como Executar Localmente

### 1. Pré-requisitos
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) instalado.
- Conta no Supabase não é necessária para o modo local.

### 2. Configurar Variáveis de Ambiente
Copie o arquivo de exemplo:
```bash
cp .env.example .env
```

Para autenticação real de vários usuários, configure o Supabase no arquivo `.env`:
```env
Authentication__Mode=Supabase
Authentication__GoogleEnabled=true
App__BaseUrl=http://localhost:5210
Database__Mode=Local
Supabase__Url=https://SEU_PROJETO.supabase.co
Supabase__AnonKey=SUA_CHAVE_ANONIMA_AQUI
```

No painel do Supabase, mantenha o provedor **Email** ativo e adicione estas Redirect URLs:

```text
http://localhost:5210/auth/callback
http://localhost:5210/redefinir-senha
```

Para ativar o Google:

1. Crie um cliente OAuth do tipo **Web application** no Google Cloud.
2. No Google, use a callback exibida em Supabase → Authentication → Providers → Google.
3. Informe o Client ID e Client Secret no provedor Google do Supabase.
4. Adicione a URL publicada do ProspectAI à lista de Redirect URLs do Supabase.

O login social está incluído no plano gratuito do Supabase dentro da franquia de usuários ativos.
Não confunda esta configuração com `GooglePlaces__ApiKey`, que pertence à busca de estabelecimentos.

O modo `Database__Mode=Local` usa SQLite mesmo quando o Supabase cuida da autenticação.
Só altere para `Database__Mode=Postgres` depois de validar `ConnectionStrings__DefaultConnection`.
Para desenvolvimento estritamente local, altere também `Authentication__Mode=Local` e configure
`LocalAuth__Email` e `LocalAuth__Password`.

> O Supabase nunca mostra senhas existentes. Se uma senha for esquecida, use o fluxo
> **Esqueceu a senha?** para criar uma nova.

### 3. Rodar a Aplicação
```bash
dotnet build ProspeccaoLeads.slnx
dotnet run --project src/ProspeccaoLeads.Web/ProspeccaoLeads.Web.csproj --urls "http://localhost:5000"
```

Acesse a aplicação no navegador em: **`http://localhost:5000`**

---

## 🧪 Testes Automatizados

Para rodar todos os testes de unidade e integração:
```bash
dotnet test ProspeccaoLeads.slnx
```

---

## 📄 Licença
Distribuído sob a licença MIT.
