# 🍽️ Anota Pedidos

Sistema completo para gerenciamento de pedidos de restaurantes, churrascarias, lanchonetes e estabelecimentos similares.

A plataforma permite que clientes realizem pedidos online e acompanhem seu andamento em tempo real, enquanto administradores gerenciam cardápio, produção, vendas e configurações do estabelecimento através de um painel administrativo moderno e responsivo.

---

## ✨ Principais Recursos

### 👤 Área do Cliente

* Visualização do cardápio por categorias
* Busca de produtos por nome
* Carrinho de compras dinâmico
* Escolha da forma de pagamento
* Pedido para consumo local ou retirada
* Acompanhamento do pedido em tempo real
* Histórico completo de pedidos

### 👨‍💼 Área Administrativa

* Gerenciamento de categorias
* Gerenciamento de produtos
* Controle de pedidos em produção
* Atualização de status dos pedidos
* Histórico de vendas
* Configuração do estabelecimento
* Gerenciamento de perfil administrativo
* Alteração segura de senha
* Notificações em tempo real

### 🔔 Comunicação em Tempo Real

Utilizando SignalR, o sistema oferece atualizações instantâneas para:

* Novos pedidos
* Mudanças de status
* Atualização automática da churrasqueira
* Notificações administrativas

---

## 🚀 Tecnologias Utilizadas

| Tecnologia            | Finalidade                |
| --------------------- | ------------------------- |
| ASP.NET Core MVC      | Backend                   |
| C#                    | Linguagem principal       |
| Entity Framework Core | ORM                       |
| MySQL                 | Banco de dados            |
| SignalR               | Comunicação em tempo real |
| HTML5                 | Estrutura                 |
| CSS3                  | Estilização               |
| JavaScript ES6+       | Interatividade            |
| FontAwesome           | Ícones                    |
| Google Fonts          | Tipografia                |

---

## 🏗️ Arquitetura do Projeto

O sistema foi desenvolvido seguindo o padrão MVC (Model-View-Controller), garantindo melhor organização, manutenção e escalabilidade.

### Estrutura Principal

```text
Anota_Pedidos/
│
├── Controllers/
├── Models/
├── Views/
├── Repository/
├── Services/
├── Data/
├── Hubs/
├── Filters/
└── wwwroot/
```

---

## 🗄️ Modelo de Dados

### Principais Entidades

| Entidade             | Descrição                |
| -------------------- | ------------------------ |
| AdminModel           | Administradores          |
| EstabelecimentoModel | Dados do estabelecimento |
| CategoriaModel       | Categorias do cardápio   |
| ProdutoModel         | Produtos                 |
| PedidoModel          | Pedidos realizados       |
| PedidoItemModel      | Itens do pedido          |
| UsuarioModel         | Clientes                 |

### Relacionamentos

```text
Estabelecimento 1 ─── N Categoria
Categoria       1 ─── N Produto
Usuario         1 ─── N Pedido
Pedido          1 ─── N PedidoItem
Produto         1 ─── N PedidoItem
```

---

## 🔒 Segurança

O sistema implementa diversas camadas de segurança:

* CSRF Protection (Anti Forgery Token)
* Rate Limiting para proteção contra força bruta
* Controle de Sessão
* HTTPS em produção
* Criptografia de dados sensíveis
* Hash seguro de senhas (BCrypt/PBKDF2)
* Controle de autenticação e autorização

---

## 📱 Responsividade

O sistema foi desenvolvido com foco em experiência mobile e desktop.

### Compatibilidade

* 📱 Smartphones
* 📲 Tablets
* 💻 Notebooks
* 🖥️ Computadores Desktop

---

## 🚦 Fluxo dos Pedidos

| Status        | Descrição                |
| ------------- | ------------------------ |
| Em Preparação | Pedido sendo produzido   |
| Pronto        | Disponível para retirada |
| Finalizado    | Pedido entregue          |

---

## ⚙️ Instalação

### Clone o projeto

```bash
git clone https://github.com/seu-usuario/anota-pedidos.git
```

### Entre na pasta

```bash
cd anota-pedidos
```

### Configure a conexão com o banco

No arquivo `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "MySqlConnection": "Server=localhost;Database=anota_pedidos;User=root;Password=sua_senha;"
  }
}
```

### Execute as migrations

```bash
dotnet ef database update
```

### Execute a aplicação

```bash
dotnet run
```

---

## 🧪 Testes Realizados

* ✔ Testes de autenticação
* ✔ Testes de autorização
* ✔ Testes de proteção CSRF
* ✔ Testes de força bruta
* ✔ Testes de sessão
* ✔ Testes de responsividade

---

## 📸 Capturas de Tela

Adicione imagens do sistema nesta seção.

```text
/docs/images/home.png
/docs/images/cardapio.png
/docs/images/pedidos.png
/docs/images/admin.png
```

---

## 🎯 Diferenciais do Projeto

* Arquitetura MVC
* Comunicação em tempo real com SignalR
* Interface responsiva
* Segurança reforçada
* Gestão completa de pedidos
* Separação em camadas
* Código escalável e de fácil manutenção

---

## 👨‍💻 Desenvolvedor

Projeto desenvolvido por [Seu Nome].

GitHub: https://github.com/seu-usuario

LinkedIn: https://linkedin.com/in/seu-perfil

---

## 📄 Licença

Este projeto está licenciado sob a licença MIT.
