﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AEGF.Dominio;
using AEGF.Dominio.Servicos;
using AEGF.Infra;
using OpenQA.Selenium;

namespace AEGF.GFViaSite
{
    public class MinhasEconomiasViaSite: AcessoSelenium, IGerenciadorFinanceiroAcesso
    {
        private GerenciadorFinanceiro _gerenciador;

        public void ProcessarContas(IEnumerable<Extrato> extratos)
        {
            IniciarBrowser();
            FazerLogin();
            PosicionarNasTransacoes();

            foreach (var extrato in extratos)
            {
                var contaDestino = _gerenciador.LerConta(extrato.Descricao).ContaDestino;

                if (String.IsNullOrEmpty(contaDestino))
                    continue;

                var gerador = new GeradorOFX(extrato, new OpcoesOFX()
                {
                    IgnorarPositivos = extrato.CartaoCredito,
                    MudaDataParaMesReferencia = extrato.CartaoCredito,
                    MultiplicarMenosUm = extrato.CartaoCredito
                });
                var arquivo = gerador.GravarTemporario();
                if (!File.Exists(arquivo)) 
                    continue;

                ClicarNoImportar();
                ClicarNoAvancar();
                ArquivoConta(arquivo, contaDestino);
            }


        }

        private void ArquivoConta(string arquivo, string contaDestino)
        {
            var janelaModal = driver.FindElement(By.CssSelector("div.x-upload-transacoes"));

            var inputFile = janelaModal.FindElement(By.CssSelector("input.x-form-file"));
            inputFile.SendKeys(arquivo);

            var inputs = janelaModal.FindElements(By.CssSelector("input.x-form-text"));
            inputs[1].SendKeys(contaDestino);
            var botao = janelaModal.FindElement(By.XPath("//button[text()='Avançar']"));
            botao.Click();
        }

        public string NomeUnico()
        {
            return "MinhasEconomiasSite";
        }

        public void Iniciar(GerenciadorFinanceiro gerenciador)
        {
            _gerenciador = gerenciador;
        }

        private void ClicarNoAvancar()
        {
            ClicaXPath("//button[text()='Avançar']");
        }

        private void ClicarNoImportar()
        {
            const string id = "ext-gen703";
            AguardarId(id);
            ClicaId(id);
        }

        private void PosicionarNasTransacoes()
        {
            const string id = "ext-gen418";
            AguardarId(id);
            ClicaId(id);
        }

        private void FazerLogin()
        {
            DigitaTextoId("email", _gerenciador.LerConfiguracao("email"));
            DigitaTextoId("senha", _gerenciador.LerConfiguracao("senha"));
            ClicaXPath("//*[@id=\"login\"]/button");
        }

        protected override string URLSite()
        {
            return "https://wwws.minhaseconomias.com.br/";
        }
    }
}