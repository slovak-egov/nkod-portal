import { useEffect, useState } from "react";

import PageHeader from "../components/PageHeader";
import Breadcrumbs from "../components/Breadcrumbs";
import MainContent from "../components/MainContent";
import PageSubheader from "../components/PageSubHeader";

import Yasgui from "@triply/yasgui";
import "@triply/yasgui/build/yasgui.min.css";
import storedQueries from '../sparql-queries.json';

//@ts-ignore
import { initAll } from  '@id-sk/frontend/idsk/all';
import { useTranslation } from "react-i18next";
import { useDocumentTitle } from "../client";

const defaultSparqlQuery = `PREFIX dcat: <http://www.w3.org/ns/dcat#>
SELECT (COUNT (*) AS ?count)
WHERE {
  ?dataset a dcat:Dataset
}
`;

let yasgui: Yasgui;

export default function Sparql()
{
      useEffect(() => {
        yasgui = new Yasgui(document.getElementById("yasgui")!, {
            "requestConfig": {
              "endpoint": () => "https://opendata.mirri.tech/api/sparql",
              "method": "GET"
            },
            "copyEndpointOnNewTab": true,
          });
          yasgui.getTab()?.setQuery(defaultSparqlQuery);
          initAll();
        return () => {};
      }, []);
      const {t} = useTranslation();
      useDocumentTitle('SPARQL');
    
    return <>
            <Breadcrumbs items={[{title: t('nkod'), link: '/'}, {title: 'SPARQL'}]} />
            <MainContent>
                <PageHeader>{t('sparqlEndpointNkod')}</PageHeader>
                <PageSubheader style={{color: '#2B8CC4', margin: '30px 0 20px 0'}}>{t('queryExamples')}</PageSubheader>

                {storedQueries.content.map((query: any, i) =><div style={{marginBottom: '20px'}} key={i}>
                    <div className="govuk-body" style={{margin: 0}}>
                        <span style={{color: '#2B8CC4', cursor: 'pointer'}} onClick={e => {
                          e.preventDefault();
                          const tab = yasgui.addTab(true);
                          if (tab) {
                            tab.setQuery(query.query);
                          }
                        }} className="govuk-link">{query.name.sk}</span>
                    </div>
                    <div className="govuk-body">
                      {query.description.sk}
                    </div>
                </div>)}

                <PageSubheader style={{color: '#2B8CC4', margin: '30px 0 20px 0'}}>Zadaj SPARQL dopyt</PageSubheader>
                <div id="yasgui" />
            </MainContent>
        </>;
}