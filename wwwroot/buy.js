const uri = "api/voucher";


$(document).ready(function() {
	$("#payment-div").hide();
	$("#claim-div").hide();

	$("#get-voucher-div").hide();
	$("#decode-invoice-button").click(function() {
		decodePayment();
	});
	//getData();
});


function _arrayBufferToBase64( buffer ) {
	var binary = '';
	var bytes = new Uint8Array( buffer );
	var len = bytes.byteLength;
	for (var i = 0; i < len; i++) {
		binary += String.fromCharCode( bytes[ i ] );
	}
	return window.btoa( binary );
}

function buyVoucher() {
	const buy_item= {
		buy_amt :$("#buy_amt").val(),
		buy_sat : $("#buy_sat").val()
	}
	$.ajax({
		type: "GET",
		url: uri + "/buy/" + buy_item.buy_amt + "/" + buy_item.buy_sat,
		cache: false,
		success: function(data) {
			console.log(data);
			$("#buy-invoice-stuff").remove();
			textfield = $("#buy-invoice-text");
			textfield.show();
			textfield.append("<span id='buy-invoice-stuff'>"+data.paymentRequest+"</span>");
			$("#voucher-buy-payreq").val(data.paymentRequest);

		},
		error: function(jqXHR, textStatus, errorThrown) {
			
			textfield = $("#buy-invoice-text");
			textfield.show();
		}

	});
}

function claimVoucher() {
	const claim_item = {
		claim_payreq: $("#voucher-buy-payreq").val()
	};
	$.ajax({
		type: "GET",
		url: uri + "/claim/" + claim_item.claim_payreq,
		cache: false,
		success: function(data) {
			console.log(data);
			if (data.errorCode === "Ok") {
				$("#claim-div").show();
				const tBody = $("#voucher-table");
				$(tBody).empty();


				$.each(data.vouchers,
					function(key, item) {
						
						restSat = item.startSat - item.usedSat;
						const tr = $("<tr></tr>")
							.append($("<td><a href=" + window.location.href + item.id + ">" +item.id.toString() + "</a></td>"))
							.append($("<td></td>").text(restSat));
						tr.appendTo(tBody);
					});
			} else {
				$("#claim-div").show();
				const tBody = $("#voucher-table");
				$(tBody).empty();
				const tr = $("<tr></tr>")
					.append($("<td></td>").text(data.errorCode));

				tr.appendTo(tBody);
			}
		},
		error: function(jqXHR, textStatus, errorThrown) {
			$("#claim-div").show();
		}

	});
}
function getData() {
  $.ajax({
    type: "GET",
    url: uri,
    cache: false,
    success: function(data) {
      const tBody = $("#voucher-table");
		
	    $("#claim-div").show();
	  console.log(data)
      $(tBody).empty();


      $.each(data, function(key, item) {
	      restSat = item.startSat - item.usedSat;
        const tr = $("<tr></tr>")
          
          .append($("<td></td>").text(item.id))
		  .append($("<td></td>").text(restSat))
          
          ;

        tr.appendTo(tBody);
      });

    }
  });
}



