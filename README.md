# ⚡ ProspectaLeads - Plataforma Inteligente de Prospecção B2B

> Sistema completo e moderno para busca, gestão, qualificação e prospecção de leads e estabelecimentos comerciais em todo o Brasil.

---

## 🌟 Funcionalidades Principais

- 🔍 **Busca Inteligente Multiprovedores**:
  - Integração com **OpenStreetMap / Overpass API** com múltiplos espelhos de contingência.
  - Motor inteligente local de catálogo comercial nacional com geolocalização e DDDs automáticos.
  - Suporte a Google Places API.
- 🎯 **Gestão e Funil de Leads (CRM)**:
  - Visualização em tabela com busca em tempo real, ordenação dinâmica e filtros por status (*Novo, Contatado, Em Negociação, Fechado, Perdido*).
  - Modal interativo com ações rápidas de contato: **WhatsApp direto**, **Ligação telefônica**, **E-mail**, **Instagram** e rota no **Google Maps**.
  - Registro de anotações internas e histórico de contato por lead.
- 📊 **Dashboard & KPIs em Tempo Real**:
  - Métricas de leads totais, taxa de conversão, leads contatados e distribuição por nicho/cidade.
- 📥 **Exportação de Dados**:
  - Exportação em formato **Excel (.xlsx)** e **CSV** estruturado.
- 🔐 **Autenticação & Segurança**:
  - Integração completa com **Supabase Auth** (Cadastro, Login, Recuperação de Senha com link seguro e Atualização de Perfil/Senha).
  - Suporte a banco de dados **PostgreSQL (Supabase)** ou **SQLite** local para desenvolvimento.

---

## 🏗️ Arquitetura e Tecnologias

- **Frontend & Backend**: [.NET 10](https://dotnet.microsoft.com/) com Blazor Server interativo.
- **Banco de Dados**: Entity Framework Core com PostgreSQL (`Npgsql`) e SQLite.
- **Autenticação & BaaS**: [Supabase](https://supabase.com/).
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
- Conta no [Supabase](https://supabase.com/) (opcional para desenvolvimento local).

### 2. Configurar Variáveis de Ambiente
Copie o arquivo de exemplo:
```bash
cp .env.example .env
```

Preencha com suas credenciais do Supabase no arquivo `.env`:
```env
SUPABASE_URL=https://seu-projeto.supabase.co
SUPABASE_ANON_KEY=sua-chave-anonima
SUPABASE_DB_URL=postgresql://postgres.seu-projeto:senha@aws-0-sa-east-1.pooler.supabase.com:6543/postgres
```

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
