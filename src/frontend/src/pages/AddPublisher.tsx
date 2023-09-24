import PageHeader from "../components/PageHeader";
import Button from "../components/Button";
import Breadcrumbs from "../components/Breadcrumbs";
import MainContent from "../components/MainContent";
import FormElementGroup from "../components/FormElementGroup";
import BaseInput from "../components/BaseInput";
import { usePublisherAdd, useUserInfo } from "../client";

export default function AddPublisher()
{
    const [userInfo] = useUserInfo();
    const [publisher, setPublisher, errors, saving, saveResult, save] = usePublisherAdd({
        website: '',
        email: '',
        phone: ''
    });

    return <>
    <Breadcrumbs items={[{title: 'Národný katalóg otvorených dát', link: '/'},{title: 'Registrácia poskytovateľa dát'}]} />
            <MainContent>
            <PageHeader>Registrácia poskytovateľa dát</PageHeader>
                    {userInfo?.companyName ? <p className="govuk-body nkod-publisher-name">
                    <span style={{color: '#2B8CC4', fontWeight: 'bold'}}>Poskytovateľ dát</span><br />
                        {userInfo.companyName}
                    </p> : null}

                    <FormElementGroup label="Adresa webového sídla" element={id => <BaseInput id={id} value={publisher.website} onChange={e => setPublisher({website: e.target.value})} />} />
                    <FormElementGroup label="E-mailová adresa kontaktnej osoby" element={id => <BaseInput id={id} value={publisher.email} onChange={e => setPublisher({email: e.target.value})} />} />
                    <FormElementGroup label="Telefónne číslo kontaktnej osoby" element={id => <BaseInput id={id} value={publisher.phone} onChange={e => setPublisher({phone: e.target.value})} />} />
                    
                    <Button style={{marginRight: '20px'}} onClick={save}>
                        Registrovať 
                    </Button>
            </MainContent>
        </>;
}