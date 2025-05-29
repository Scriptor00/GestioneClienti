const connection = new signalR.HubConnectionBuilder()
    .withUrl("/disponibilitaHub")
    .build();

connection.on("RiceviAggiornamento", (prodottoId, nuovaQuantita) => {
    console.log(`Prodotto ${prodottoId} ha nuova quantità: ${nuovaQuantita}`);
    const elem = document.getElementById(`disponibilita-${prodottoId}`);
    if (elem) {
        elem.innerText = nuovaQuantita;
    }
});

connection.start().catch(err => console.error("Errore avvio SignalR: ", err));
