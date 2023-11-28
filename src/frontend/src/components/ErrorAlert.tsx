import { useTranslation } from "react-i18next";
import Alert from "./Alert";

type Props = {
    error: Error;
}

export default function ErrorAlert(props: Props)
{
    const {t} = useTranslation();
    
    return <Alert type="warning">
        <div style={{padding: '5px 10px'}}>
            {t('loadingError')}
        </div>
    </Alert>
}