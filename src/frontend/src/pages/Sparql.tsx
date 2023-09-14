import { useEffect, useState } from "react";

import PageHeader from "../components/PageHeader";
import Breadcrumbs from "../components/Breadcrumbs";
import MainContent from "../components/MainContent";
import PageSubheader from "../components/PageSubHeader";

import Yasgui from "@triply/yasgui";
import "@triply/yasgui/build/yasgui.min.css";

//@ts-ignore
import { initAll } from  '@id-sk/frontend/idsk/all';

const defaultLanguage = "sk";

const defaultSparqlQuery = `PREFIX dcat: <http://www.w3.org/ns/dcat#>
PREFIX dct: <http://purl.org/dc/terms/>
PREFIX foaf: <http://xmlns.com/foaf/0.1/>

SELECT ?poskytovateľ (COUNT(DISTINCT ?dataset) AS ?počet) WHERE {
  GRAPH ?g {
    ?dataset a dcat:Dataset;
      dct:publisher/foaf:name ?poskytovateľ.
    FILTER(langMatches(LANG(?poskytovateľ), "sk"))
  }
}
GROUP BY ?poskytovateľ      
ORDER BY DESC(?počet)
`;

export default function Sparql()
{
    useEffect(() => {
        const yasgui = new Yasgui(document.getElementById("yasgui")!, {
            "requestConfig": {
              "endpoint": () => "https://opendata.mirri.tech/api/sparql",
              "method": "GET"
            },
            "copyEndpointOnNewTab": false,
          });
          initAll();
        return () => {};
      }, []);
    
    return <>
            <Breadcrumbs items={[{title: 'Národný katalóg otvorených dát', link: '/'}, {title: 'SPARQL'}]} />
            <MainContent>
                <PageHeader>SPARQL Endpoint pre Národný katalóg otvorených dát</PageHeader>
                <PageSubheader style={{color: '#2B8CC4', margin: '30px 0 20px 0'}}>Príklady dotazov</PageSubheader>

                <div style={{marginBottom: '20px'}}>
                    <div className="govuk-body" style={{margin: 0}}>
                        <a href="#" className="govuk-link">100 datasetov a ich poskytovateľov</a>
                    </div>
                    <div className="govuk-body">
                        100 datasetov a ich poskytovateľov
                    </div>
                </div>
                <div style={{marginBottom: '20px'}}>
                    <div className="govuk-body" style={{margin: 0}}>
                        <a href="#" className="govuk-link">Zoznam lokálnych katalógov údajov</a>
                    </div>
                    <div className="govuk-body">
                    Zoznam lokálnych dátových katalógov a počty dátových sád v nich
                    </div>
                </div>
                <div style={{marginBottom: '20px'}}>
                    <div className="govuk-body" style={{margin: 0}}>
                        <a href="#" className="govuk-link">Počet datasetov podľa poskytovateľa</a>
                    </div>
                    <div className="govuk-body">
                    Počet datasetov podľa poskytovateľa
                    </div>
                </div>


                <PageSubheader style={{color: '#2B8CC4', margin: '30px 0 20px 0'}}>Zadaj SPARQL dopyt</PageSubheader>
                <div id="yasgui" />
            </MainContent>
        </>;
}