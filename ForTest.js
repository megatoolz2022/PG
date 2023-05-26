
$( document ).ready(function() {
	"use strict";

	// faqs filter
	$("#live-faqs-search").on("keyup", function() {
		var value = $(this).val().toLowerCase();
		$("#live-faqs-list .uk-accordion-title").each(function() {
			var toggle = $(this).text().toLowerCase().indexOf(value) > -1;
			$(this).closest('li').toggle(toggle);
		});
	});

	// search box autocomplate
	var options = {
		url: "http://uikitbase.com/demo/html/uhow/resources/search-results.json",

	    getValue: "text",

		list: {
			showAnimation: {
				type: "fade",
				time: 300,
				callback: function() {}
			},

			hideAnimation: {
				type: "fade",
				time: 300,
				callback: function() {}
			},

			match: {
				enabled: true
			},

			maxNumberOfElements: 5,
		},

	    template: {
	        type: "links",
	        fields: {
	            link: "website-link"
	        }
	    },
	};

	$("#autocomplete-input").easyAutocomplete(options);
});
