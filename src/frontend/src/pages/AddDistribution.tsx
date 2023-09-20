import { useDatasetAdd, useDistributionAdd, useDistributions, useUserInfo } from "../client";

import PageHeader from "../components/PageHeader";
import Breadcrumbs from "../components/Breadcrumbs";
import MainContent from "../components/MainContent";
import Button from "../components/Button";
import ValidationSummary from "../components/ValidationSummary";
import { DatasetForm } from "../components/DatasetForm";
import { DistributionForm } from "../components/DistributionForm";
import { useParams } from "react-router";


export default function AddDistribution()
{
    const { datasetId } = useParams();

    const [distribution, setDistribution, errors, saving, saveResult, save] = useDistributionAdd({
        datasetId: datasetId ?? '',
        authorsWorkType: 'https://data.gov.sk/def/authors-work-type/3',
        originalDatabaseType: 'https://data.gov.sk/def/original-database-type/3',
        databaseProtectedBySpecialRightsType: 'https://data.gov.sk/def/codelist/database-creator-special-rights-type/2',
        personalDataContainmentType: 'https://data.gov.sk/def/personal-data-occurence-type/2',
        downloadUrl: null,
        accessUrl: null,
        format: null,
        mediaType: null,
        compressFormat: null,
        packageFormat: null,
        conformsTo: null,
        title: null,
        fileId: null
    });

    const [userInfo] = useUserInfo();

    return <>
            <Breadcrumbs items={[{title: 'Národný katalóg otvorených dát', link: '/'}, {title: 'Zoznam distribúcií', link: '/sprava/distributions'}, {title: 'Nová distribúcia'}]} />
            <MainContent>
                <div className="nkod-form-page">
                    <PageHeader>Nová distribúcia</PageHeader>
                    {userInfo?.publisherView ? <p className="govuk-body nkod-publisher-name">
                    <span style={{color: '#2B8CC4', fontWeight: 'bold'}}>Poskytovateľ dát</span><br />
                        {userInfo.publisherView.name}
                    </p> : null}

                    {Object.keys(errors).length > 0 ? <ValidationSummary elements={Object.entries(errors).map(k => ({
                        elementId: k[0],
                        message: k[1]
                    }))} /> : null}

                    <DistributionForm distribution={distribution} setDistribution={setDistribution} errors={errors} userInfo={userInfo} />

                    <Button style={{marginRight: '20px'}} onClick={save} disabled={saving}>
                        Uložiť distribúciu
                    </Button>
                </div>
            </MainContent>
        </>;
}