(function ($) {
    $(document).ready(function () {

        let $openSourceList = $("#openSourceList");
        if ($openSourceList.length > 0) {
            let template = _.template(
                "<div class='e-card'>" +
                "<div tabindex='0' class='e-card-about' id='<%= name %>'>" +
                "   <div class='card-header'>" +
                "     <div class='e-card-header-caption'>" +
                "       <h4 class='e-card-header-caption-title'><a href='<%= html_url %>' target'_blank'><%= name %></a></h4>" +
                "     </div>" +
                "   </div>" +
                "   <div class='e-card-content font-gray'>" +
                "           <div class='font-gray'><%= description %></div>" +
                "       </div>" +
                " </div>" +
                "</div>");
            $.get("https://api.github.com/users/saigkill/repos?type=owner&sort-updated")
                .then(function (result) {
                    let results = _.filter(result, function (item) {
                        return !item.fork;
                    });
                    results = _.orderBy(results, ["stargazers_count"], ["desc"]);
                    _.forEach(results, function (item) {
                        $openSourceList.append($(template(item)))
                    });
                });
        }
    });
})(jQuery);