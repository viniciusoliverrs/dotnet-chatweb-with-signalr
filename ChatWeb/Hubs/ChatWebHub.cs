using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ChatWeb.Models;
using ChatWeb.Models.Context;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace ChatWeb.Hubs
{
    public class ChatWebHub : Hub
    {
        private ChatContext _banco;
        public ChatWebHub(ChatContext banco)
        {
            _banco = banco;
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var user = _banco.Users.FirstOrDefault(a => a.ConnectionId.Contains(Context.ConnectionId));
            if (user != null)
            {
                await DelConnectionIdDoUsuario(user);
            }

            await base.OnDisconnectedAsync(exception);
        }
        public async Task Cadastrar(User usuario)
        {
            bool IsExistUser = _banco.Users.Where(a => a.Email == usuario.Email).Count() > 0;

            if (IsExistUser)
            {
                await Clients.Caller.SendAsync("ReceberCadastro", false, null, "E-mail já cadastrado!");
            }
            else
            {
                _banco.Users.Add(usuario);
                _banco.SaveChanges();

                await Clients.Caller.SendAsync("ReceberCadastro", true, usuario, "Usuário cadastrado com sucesso!");
            }
        }

        public async Task Login(User usuario)
        {
            var usuarioDB = _banco.Users.FirstOrDefault(a => a.Email == usuario.Email && a.Password == usuario.Password);

            if (usuarioDB == null)
            {
                await Clients.Caller.SendAsync("ReceberLogin", false, null, "E-mail ou senha errado!");
            }
            else
            {
                await Clients.Caller.SendAsync("ReceberLogin", true, usuarioDB, null);

                usuarioDB.IsOnline = true;
                _banco.Users.Update(usuarioDB);
                _banco.SaveChanges();


                await NotificarMudancaNaListaUsers();
            }
        }

        public async Task Logout(User usuario)
        {
            var usuarioDB = _banco.Users.Find(usuario.Id);
            usuarioDB.IsOnline = false;
            _banco.Users.Update(usuarioDB);
            _banco.SaveChanges();

            await DelConnectionIdDoUsuario(usuarioDB);

            await NotificarMudancaNaListaUsers();

        }

        public async Task AddConnectionIdDoUsuario(User usuario)
        {
            var ConnectionIdCurrent = Context.ConnectionId;
            List<string> connectionsId = null;

            User usuarioDB = _banco.Users.Find(usuario.Id);
            if (usuarioDB.ConnectionId == null)
            {
                connectionsId = new List<string>();
                connectionsId.Add(ConnectionIdCurrent);
            }
            else
            {
                connectionsId = JsonConvert.DeserializeObject<List<string>>(usuarioDB.ConnectionId);
                if (!connectionsId.Contains(ConnectionIdCurrent))
                {
                    connectionsId.Add(ConnectionIdCurrent);
                }
            }

            usuarioDB.IsOnline = true;
            usuarioDB.ConnectionId = JsonConvert.SerializeObject(connectionsId);
            _banco.Users.Update(usuarioDB);
            _banco.SaveChanges();
            await NotificarMudancaNaListaUsers();

            //Adicionar ConnectionsId aos grupos do SignalR
            var grupos = _banco.Grupos.Where(a => a.Users.Contains(usuarioDB.Email));
            foreach (var connectionId in connectionsId)
            {
                foreach (var grupo in grupos)
                {
                    await Groups.AddToGroupAsync(connectionId, grupo.Name);
                }
            }
        }

        public async Task DelConnectionIdDoUsuario(User usuario)
        {
            User usuarioDB = _banco.Users.Find(usuario.Id);
            List<string> connectionsId = null;
            if (usuarioDB.ConnectionId.Length > 0)
            {
                var ConnectionIdCurrent = Context.ConnectionId;

                connectionsId = JsonConvert.DeserializeObject<List<string>>(usuarioDB.ConnectionId);
                if (connectionsId.Contains(ConnectionIdCurrent))
                {
                    connectionsId.Remove(ConnectionIdCurrent);
                }
                usuarioDB.ConnectionId = JsonConvert.SerializeObject(connectionsId);

                if (connectionsId.Count <= 0)
                {
                    usuarioDB.IsOnline = false;

                }

                _banco.Users.Update(usuarioDB);
                _banco.SaveChanges();
                await NotificarMudancaNaListaUsers();


                //Remoção da ConnectionId dos Grupos de conversa desse usuário no SignalR.
                var grupos = _banco.Grupos.Where(a => a.Users.Contains(usuarioDB.Email));
                foreach (var connectionId in connectionsId)
                {
                    foreach (var grupo in grupos)
                    {
                        await Groups.RemoveFromGroupAsync(connectionId, grupo.Name);
                    }
                }
            }


        }

        public async Task ObterListaUsers()
        {
            var Users = _banco.Users.ToList();
            await Clients.Caller.SendAsync("ReceberListaUsers", Users);
        }
        public async Task NotificarMudancaNaListaUsers()
        {
            var Users = _banco.Users.ToList();
            await Clients.All.SendAsync("ReceberListaUsers", Users);
        }

        /*
         SignalR - 
         elias@gmail.com - aline@gmail.com = elias@gmail.com-aline@gmail.com
         aline@gmail.com - elias@gmail.com = aline@gmail.com-elias@gmail.com 

        */
        public async Task CriarOuAbrirGrupo(string emailUserUm, string emailUserDois)
        {
            string nomeGrupo = CriarNomeGrupo(emailUserUm, emailUserDois);

            Group grupo = _banco.Grupos.FirstOrDefault(a => a.Name == nomeGrupo);
            if (grupo == null)
            {
                grupo = new Group();
                grupo.Name = nomeGrupo;
                grupo.Users = JsonConvert.SerializeObject(new List<string>()
                {
                    emailUserUm,
                    emailUserDois
                });

                _banco.Grupos.Add(grupo);
                _banco.SaveChanges();
            }

            //Adicionou as Connections Ids para o Grupo no SignalR
            List<string> emails = JsonConvert.DeserializeObject<List<string>>(grupo.Users);
            List<User> Users = new List<User>() {
                _banco.Users.First(a => a.Email == emails[0]),
                _banco.Users.First(a => a.Email == emails[1])
            };

            foreach (var usuario in Users)
            {
                var connectionsId = JsonConvert.DeserializeObject<List<string>>(usuario.ConnectionId);
                foreach (var connectionId in connectionsId)
                {
                    await Groups.AddToGroupAsync(connectionId, nomeGrupo);
                }
            }

            var mensagens = _banco.Messages.Where(a => a.GroupName == nomeGrupo).OrderBy(a => a.CreatedDate).ToList();
            for (int i = 0; i < mensagens.Count; i++)
            {
                mensagens[i].User = JsonConvert.DeserializeObject<User>(mensagens[i].JsonUser);
            }
            await Clients.Caller.SendAsync("AbrirGrupo", nomeGrupo, mensagens);
        }
        public async Task EnviarMensagem(User usuario, string msg, string nomeGrupo)
        {
            Group grupo = _banco.Grupos.FirstOrDefault(a => a.Name == nomeGrupo);

            if (!grupo.Users.Contains(usuario.Email))
            {
                throw new Exception("Usuário não pertence ao grupo!");
            }

            Message mensagem = new Message();
            mensagem.GroupName = nomeGrupo;
            mensagem.Text = msg;
            mensagem.UserId = usuario.Id;
            mensagem.JsonUser = JsonConvert.SerializeObject(usuario);
            mensagem.User = usuario;
            mensagem.CreatedDate = DateTime.Now;

            _banco.Messages.Add(mensagem);
            _banco.SaveChanges();

            await Clients.Group(nomeGrupo).SendAsync("ReceberMensagem", mensagem, nomeGrupo);
        }


        private string CriarNomeGrupo(string emailUserUm, string emailUserDois)
        {
            List<string> lista = new List<string>() { emailUserUm, emailUserDois };
            var listaOrdernada = lista.OrderBy(a => a).ToList();

            StringBuilder sb = new StringBuilder();
            foreach (var item in listaOrdernada)
            {
                sb.Append(item);
            }

            return sb.ToString();
        }
    }
}