import PageHeader from "../components/PageHeader";
import Button from "../components/Button";
import Breadcrumbs from "../components/Breadcrumbs";
import MainContent from "../components/MainContent";
import FormElementGroup from "../components/FormElementGroup";
import BaseInput from "../components/BaseInput";

export default function PublisherProfile()
{
    return <>
    <Breadcrumbs items={[{title: 'Národný katalóg otvorených dát', link: '/'},{title: 'Úprava profilu poskytovateľa dát'}]} />
            <MainContent>
            <PageHeader>Úprava profilu poskytovateľa dát</PageHeader>
                <p className="govuk-body nkod-publisher-name">
                    <span style={{color: '#2B8CC4', fontWeight: 'bold'}}>Poskytovateľ dát</span><br />
                        Ministerstvo investícií, regionálneho rozvoja a informatizácie Slovenskej republiky 
                    </p>
                    <FormElementGroup label="Adresa webového sídla" element={id => <BaseInput id={id} />} />
                    <FormElementGroup label="E-mailová adresa kontaktnej osoby" element={id => <BaseInput id={id} />} />
                    <FormElementGroup label="Telefónne číslo kontaktnej osoby" element={id => <BaseInput id={id} />} />
                    
                    <Button style={{marginRight: '20px'}}>
                        Uložiť 
                    </Button>
            </MainContent>
        </>;
}