# Relatório - Sistema de Redes para Jogos

### Tema: ShipsHappen (Batalha Naval)

_Simão Campaniço a22510616_

---

### Descrição do Projeto

Jogo de batalha naval online. Dois jogadores conectam-se através de um código de sala, colocam os seus barcos numa grelha 10x10 e, alternando turnos, tentam adivinhar e destruir os barcos do adversário. O jogo conta com autenticação de utilizadores e uma leaderboard global.

---

### Link para o Projeto

- **GitHub (Source):** https://github.com/SimaoCampanicoDev/ShipsHappen

#### Instruções

1. Ambos os jogadores abrem o executável (ou um usa o Editor e o outro o executável)
2. Fazem login ou registam uma conta na **LoginScene**
3. Um jogador clica **Host** e aparece um código de sala
4. O segundo jogador clica **Join** e introduz o código
5. Ambos são enviados para a **PlacementScene** automaticamente
6. Depois de inserirem todos os barcos e clicarem em confirm, são enviados para a **GameScene** onde podem começar, por turnos, a adivinhar os barcos de cada um.

---

### Timeline

### 18/05/2026

A procurar uma ideia para o jogo, joguei um jogo chamado BattleTabs (https://discord.com/application-directory/battletabs), um jogo de batalha naval que está disponível no Discord. Gostei do conceito e comecei a fazer a pesquisa, onde procurei logo por uma imagem de referência para o layout da grelha, até porque eu não sabia sequer o tamanho da mesma, até encontrar a imagem que me ajudou (https://www.digitall.vodafone.pt/wp-content/uploads/2021/07/DA024L1.1-1024x576.jpg). Após isso, comecei por usar o Unity Netcode (https://youtu.be/3yuBOB3VrCk?si=NB_LHXBzdzy4IMHV) e o Unity Relay (https://youtu.be/msPNJ2cxWfw?si=fpNF54Y6aT4eOamT), e ao mesmo tempo que procurava pelos dois encontrei um vídeo que juntava os dois, tanto a programação de um jogo de batalha naval 2D como a parte de fazer um jogo Online no Unity. Tutorial parte 1 (https://www.youtube.com/watch?v=s3ZrQbI5o_k) e parte 2 (https://youtu.be/3Fw-NiZ3HGg?si=J4osvecTw9GkBtOQ). O link do github da minha inspiração para a client side do jogo (https://github.com/mobyjames/naval-battle-client).

### 28/05/2026

Criação do projeto Unity com `.gitignore` e `README.md` e conectando o projeto ao GitHub.

### 31/05/2026

Instalação dos packages necessários:

Netcode for GameObjects
Unity Transport
Multiplayer Services
Authentication

Criação do `LobbyManager.cs` com base no `NetworkSetup.cs` do projeto do professor. Implementação dos botões Host e Join com comunicação usando o Unity Relay (ativado no https://cloud.unity.com/). Testes ao MainMenu para, quando houver 2 pessoas na sala mudar para a GameScene.