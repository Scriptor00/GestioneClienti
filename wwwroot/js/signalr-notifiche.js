const connection = new signalR.HubConnectionBuilder()
    .withUrl("/disponibilitaHub")
    .build();

connection.on("RiceviAggiornamento", (prodottoId, nuovaQuantitaOrdinabile, nuovaPrenotataDaAltri, nuovaGiacenzaReale) => {
    console.log(`Prodotto ${prodottoId} aggiornato: Ordinabile=${nuovaQuantitaOrdinabile}, PrenotataDaAltri=${nuovaPrenotataDaAltri}, Giacenza=${nuovaGiacenzaReale}`);

    const ordinabileElem = document.getElementById(`quantita-ordinabile-display-${prodottoId}`);
    if (ordinabileElem) {
        ordinabileElem.textContent = nuovaQuantitaOrdinabile;
    }

    const prenotataAltriElem = document.getElementById(`quantita-prenotata-altri-${prodottoId}`);
    if (prenotataAltriElem) {
        prenotataAltriElem.textContent = nuovaPrenotataDaAltri;
    }

    const giacenzaElem = document.getElementById(`giacenza-reale-${prodottoId}`);
    if (giacenzaElem) {
        giacenzaElem.textContent = nuovaGiacenzaReale;
    }

    const inputElem = document.querySelector(`input[data-product-id="${prodottoId}"]`);
    if (inputElem) {
        inputElem.max = nuovaQuantitaOrdinabile;
    }
});


connection.start().catch(err => console.error("Errore avvio SignalR: ", err));
